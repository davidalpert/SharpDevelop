﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Debugger.Wrappers.CorDebug;

namespace Debugger
{
	public partial class Thread: DebuggerObject
	{
		Process process;
		
		ICorDebugThread corThread;
		
		Exception     currentException;
		DebuggeeState currentException_DebuggeeState;
		ExceptionType currentExceptionType;
		bool          currentExceptionIsUnhandled;

		List<Stepper> steppers = new List<Stepper>();

		uint id;
		bool lastSuspendedState = false;
		ThreadPriority lastPriority = ThreadPriority.Normal;
		string lastName = string.Empty;
		bool hasBeenLoaded = false;
		
		bool hasExpired = false;
		bool nativeThreadExited = false;

		StackFrame selectedStackFrame;
		
		public event EventHandler Expired;
		public event EventHandler<ThreadEventArgs> NativeThreadExited;
		
		public bool HasExpired {
			get {
				return hasExpired;
			}
		}
		
		[Debugger.Tests.Ignore]
		public Process Process {
			get {
				return process;
			}
		}

		internal bool HasBeenLoaded {
			get {
				return hasBeenLoaded;
			}
			set {
				hasBeenLoaded = value;
				OnStateChanged();
			}
		}
		
		[Debugger.Tests.Ignore]
		public uint ID { 
			get{ 
				return id; 
			} 
		}
		
		/// <summary> If the thread is not at safe point, it is not posible to evaluate
		/// on it </summary>
		/// <remarks> Returns false is the thread is in invalid state </remarks>
		public bool IsAtSafePoint {
			get {
				if (IsInValidState) {
					return CorThread.UserState != CorDebugUserState.USER_UNSAFE_POINT;
				} else {
					return false;
				}
			}
		}
		
		/// <summary>
		/// From time to time the thread may be in invalid state.
		/// </summary>
		public bool IsInValidState {
			get {
				try {
					CorThread.UserState.ToString();
					return true;
				} catch (COMException e) {
					// The state of the thread is invalid.
					if ((uint)e.ErrorCode == 0x8013132D) {
						return false;
					}
					throw;
				}
			}
		}
		
		internal ICorDebugThread CorThread {
			get {
				if (nativeThreadExited) {
					throw new DebuggerException("Native thread has exited");
				}
				return corThread;
			}
		}
		
		internal Thread(Process process, ICorDebugThread corThread)
		{
			this.process = process;
			this.corThread = corThread;
			this.id = CorThread.ID;
		}
		
		void Expire()
		{
			System.Diagnostics.Debug.Assert(!this.hasExpired);
			
			process.TraceMessage("Thread " + this.ID + " expired");
			this.hasExpired = true;
			OnExpired(new ThreadEventArgs(this));
			if (process.SelectedThread == this) {
				process.SelectedThread = null;
			}
		}
		
		protected virtual void OnExpired(EventArgs e)
		{
			if (Expired != null) {
				Expired(this, e);
			}
		}
		
		internal void NotifyNativeThreadExited()
		{
			if (!this.hasExpired) Expire();
			
			nativeThreadExited = true;
			OnNativeThreadExited(new ThreadEventArgs(this));
		}
		
		protected virtual void OnNativeThreadExited(ThreadEventArgs e)
		{
			if (NativeThreadExited != null) {
				NativeThreadExited(this, e);
			}
		}
		
		public bool Suspended {
			get {
				if (process.IsRunning) return lastSuspendedState;
				
				lastSuspendedState = (CorThread.DebugState == CorDebugThreadState.THREAD_SUSPEND);
				return lastSuspendedState;
			}
			set {
				CorThread.SetDebugState((value==true)?CorDebugThreadState.THREAD_SUSPEND:CorDebugThreadState.THREAD_RUN);
			}
		}
		
		public ThreadPriority Priority {
			get {
				if (!HasBeenLoaded) return lastPriority;
				if (process.IsRunning) return lastPriority;

				Value runTimeValue = RuntimeValue;
				if (runTimeValue.IsNull) return ThreadPriority.Normal;
				lastPriority = (ThreadPriority)(int)runTimeValue.GetMemberValue("m_Priority").PrimitiveValue;
				return lastPriority;
			}
		}

		public Value RuntimeValue {
			get {
				// TODO: It may have started - rework
				if (!HasBeenLoaded) throw new DebuggerException("Thread has not started jet");
				process.AssertPaused();
				
				return new Value(process, CorThread.Object);
			}
		}
		
		public string Name {
			get {
				if (!HasBeenLoaded) return lastName;
				if (process.IsRunning) return lastName;
				Value runtimeValue  = RuntimeValue;
				if (runtimeValue.IsNull) return lastName;
				Value runtimeName = runtimeValue.GetMemberValue("m_Name");
				if (runtimeName.IsNull) return string.Empty;
				lastName = runtimeName.AsString.ToString();
				return lastName;
			}
		}
		
		public Exception CurrentException {
			get {
				if (currentException_DebuggeeState == this.Process.DebuggeeState) {
					return currentException;
				} else {
					return null;
				}
			}
			internal set { currentException = value; }
		}
		
		internal DebuggeeState CurrentException_DebuggeeState {
			get { return currentException_DebuggeeState; }
			set { currentException_DebuggeeState = value; }
		}
		
		public ExceptionType CurrentExceptionType {
			get { return currentExceptionType; }
			internal set { currentExceptionType = value; }
		}
		
		public bool CurrentExceptionIsUnhandled {
			get { return currentExceptionIsUnhandled; }
			internal set { currentExceptionIsUnhandled = value; }
		}
		
		/// <summary> Tryies to intercept the current exception.
		/// The intercepted expression stays available through the CurrentException property. </summary>
		/// <returns> False, if the exception was already intercepted or 
		/// if it can not be intercepted. </returns>
		public bool InterceptCurrentException()
		{
			if (!this.CorThread.Is<ICorDebugThread2>()) return false; // Is the debuggee .NET 2.0?
			if (this.CorThread.CurrentException == null) return false; // Is there any exception
			if (this.MostRecentStackFrame == null) return false; // Is frame available?  It is not at StackOverflow
			
			try {
				// Interception will expire the CorValue so keep permanent reference
				currentException.MakeValuePermanent();
				this.CorThread.CastTo<ICorDebugThread2>().InterceptCurrentException(this.MostRecentStackFrame.CorILFrame.CastTo<ICorDebugFrame>());
			} catch (COMException e) {
				// 0x80131C02: Cannot intercept this exception
				if ((uint)e.ErrorCode == 0x80131C02) {
					return false;
				}
				throw;
			}
			
			Process.AsyncContinue_KeepDebuggeeState();
			Process.WaitForPause();
			return true;
		}
		
		internal Stepper GetStepper(ICorDebugStepper corStepper)
		{
			foreach(Stepper stepper in steppers) {
				if (stepper.IsCorStepper(corStepper)) {
					return stepper;
				}
			}
			throw new DebuggerException("Stepper is not in collection");
		}
		
		internal List<Stepper> Steppers {
			get {
				return steppers;
			}
		}
		
		public event EventHandler<ThreadEventArgs> StateChanged;
		
		protected void OnStateChanged()
		{
			if (StateChanged != null) {
				StateChanged(this, new ThreadEventArgs(this));
			}
		}
		
		
		public override string ToString()
		{
			return String.Format("Thread Name = {1} Suspended = {2}", ID, Name, Suspended);
		}
		
		/// <summary> Gets the whole callstack of the Thread. </summary>
		/// <remarks> If the thread is in invalid state returns empty array </remarks>
		public StackFrame[] GetCallstack()
		{
			return new List<StackFrame>(CallstackEnum).ToArray();
		}
		
		/// <summary> Get given number of frames from the callstack </summary>
		public StackFrame[] GetCallstack(int maxFrames)
		{
			List<StackFrame> frames = new List<StackFrame>();
			foreach(StackFrame frame in CallstackEnum) {
				frames.Add(frame);
				if (frames.Count == maxFrames) break;
			}
			return frames.ToArray();
		}
		
		IEnumerable<StackFrame> CallstackEnum {
			get {
				process.AssertPaused();
				
				if (!IsInValidState) {
					yield break;
				}
				
				int depth = 0;
				foreach(ICorDebugChain corChain in CorThread.EnumerateChains().Enumerator) {
					if (corChain.IsManaged == 0) continue; // Only managed ones
					foreach(ICorDebugFrame corFrame in corChain.EnumerateFrames().Enumerator) {
						if (corFrame.Is<ICorDebugILFrame>()) {
							StackFrame stackFrame;
							try {
								stackFrame = new StackFrame(this, corFrame.CastTo<ICorDebugILFrame>(), depth);
								depth++;
							} catch (COMException) { // TODO
								continue;
							};
							yield return stackFrame;
						}
					}
				}
			}
		}
		
		public string GetStackTrace()
		{
			return GetStackTrace("at {0} in {1}:line {2}", "at {0} in {1}");
		}
		
		public string GetStackTrace(string formatSymbols, string formatNoSymbols)
		{
			StringBuilder stackTrace = new StringBuilder();
			foreach(StackFrame stackFrame in this.GetCallstack(100)) {
				SourcecodeSegment loc = stackFrame.NextStatement;
				if (loc != null) {
					stackTrace.Append("   ");
					stackTrace.AppendFormat(formatSymbols, stackFrame.MethodInfo.FullName, loc.SourceFullFilename, loc.StartLine);
					stackTrace.AppendLine();
				} else {
					stackTrace.AppendFormat(formatNoSymbols, stackFrame.MethodInfo.FullName);
					stackTrace.AppendLine();
				}
			}
			return stackTrace.ToString();
		}
		
		public StackFrame SelectedStackFrame {
			get {
				if (selectedStackFrame != null && selectedStackFrame.HasExpired) return null;
				if (process.IsRunning) return null;
				return selectedStackFrame;
			}
			set {
				if (value != null && !value.HasSymbols) {
					throw new DebuggerException("SelectedFunction must have symbols");
				}
				selectedStackFrame = value;
			}
		}
		
		#region Convenience methods
		
		public StackFrame MostRecentStackFrameWithLoadedSymbols {
			get {
				foreach (StackFrame stackFrame in CallstackEnum) {
					if (stackFrame.HasSymbols) {
						return stackFrame;
					}
				}
				return null;
			}
		}
		
		/// <summary>
		/// Returns the most recent stack frame (the one that is currently executing).
		/// Returns null if callstack is empty.
		/// </summary>
		public StackFrame MostRecentStackFrame {
			get {
				foreach(StackFrame stackFrame in CallstackEnum) {
					return stackFrame;
				}
				return null;
			}
		}
		
		/// <summary>
		/// Returns the first stack frame that was called on thread
		/// </summary>
		public StackFrame OldestStackFrame {
			get {
				StackFrame first = null;
				foreach(StackFrame stackFrame in CallstackEnum) {
					first = stackFrame;
				}
				return first;
			}
		}
		
		#endregion
		
		public bool IsMostRecentStackFrameNative {
			get {
				process.AssertPaused();
				if (this.IsInValidState) {
					return corThread.ActiveChain.IsManaged == 0;
				} else {
					return false;
				}
			}
		}
	}
	
	[Serializable]
	public class ThreadEventArgs : ProcessEventArgs
	{
		Thread thread;
		
		[Debugger.Tests.Ignore]
		public Thread Thread {
			get {
				return thread;
			}
		}
		
		public ThreadEventArgs(Thread thread): base(thread.Process)
		{
			this.thread = thread;
		}
	}
}
