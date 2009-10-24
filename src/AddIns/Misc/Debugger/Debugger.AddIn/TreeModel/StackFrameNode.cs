// <file>
//     <copyright license="BSD-new" see="prj:///COPYING"/>
//     <owner name="David Srbeck�" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using Debugger.MetaData;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;

namespace Debugger.AddIn.TreeModel
{
	public class StackFrameNode: TreeNode
	{
		StackFrame stackFrame;
		
		public StackFrame StackFrame {
			get { return stackFrame; }
		}
		
		public StackFrameNode(StackFrame stackFrame)
		{
			this.stackFrame = stackFrame;
			
			this.Name = stackFrame.MethodInfo.Name;
			this.ChildNodes = LazyGetChildNodes();
		}
		
		IEnumerable<TreeNode> LazyGetChildNodes()
		{
			foreach(DebugParameterInfo par in stackFrame.MethodInfo.GetParameters()) {
				yield return new ExpressionNode(ExpressionNode.GetImageForParameter(), par.Name, new IdentifierExpression(par.Name));
			}
			foreach(DebugLocalVariableInfo locVar in stackFrame.MethodInfo.GetLocalVariables()) {
				yield return new ExpressionNode(ExpressionNode.GetImageForLocalVariable(), locVar.Name, new IdentifierExpression(locVar.Name));
			}
			if (stackFrame.Thread.CurrentException != null) {
				yield return new ExpressionNode(null, "__exception", new IdentifierExpression("__exception"));
			}
		}
	}
}