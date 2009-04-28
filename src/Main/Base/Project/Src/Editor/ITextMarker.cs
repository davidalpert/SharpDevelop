// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ICSharpCode.SharpDevelop.Editor
{
	/// <summary>
	/// Represents a text marker.
	/// </summary>
	public interface ITextMarker
	{
		/// <summary>
		/// Gets the start offset of the marked text region.
		/// </summary>
		int StartOffset { get; }
		
		/// <summary>
		/// Gets the end offset of the marked text region.
		/// </summary>
		int EndOffset { get; }
		
		/// <summary>
		/// Gets the length of the marked region.
		/// </summary>
		int Length { get; }
		
		/// <summary>
		/// Deletes the text marker.
		/// </summary>
		void Delete();
		
		/// <summary>
		/// Gets whether the text marker was deleted.
		/// </summary>
		bool IsDeleted { get; }
		
		/// <summary>
		/// Event that occurs when the text marker is deleted.
		/// </summary>
		event EventHandler Deleted;
		
		/// <summary>
		/// Gets/Sets the background color.
		/// </summary>
		Color? BackgroundColor { get; set; }
		
		/// <summary>
		/// Gets/Sets the foreground color.
		/// </summary>
		Color? ForegroundColor { get; set; }
	}
	
	public interface ITextMarkerService
	{
		ITextMarker Create(int startOffset, int length);
		
		/// <summary>
		/// Gets the list of text markers.
		/// </summary>
		IEnumerable<ITextMarker> TextMarkers { get; }
	}
}