// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using System.Windows;

namespace ICSharpCode.AvalonEdit.Editing
{
	static class CaretNavigationCommandHandler
	{
		/// <summary>
		/// Creates a new <see cref="TextAreaInputHandler"/> for the text area.
		/// </summary>
		public static TextAreaInputHandler Create(TextArea textArea)
		{
			TextAreaInputHandler handler = new TextAreaInputHandler(textArea);
			handler.CommandBindings.AddRange(CommandBindings);
			handler.InputBindings.AddRange(InputBindings);
			return handler;
		}
		
		static readonly List<CommandBinding> CommandBindings = new List<CommandBinding>();
		static readonly List<InputBinding> InputBindings = new List<InputBinding>();
		
		static void AddBinding(ICommand command, ModifierKeys modifiers, Key key, ExecutedRoutedEventHandler handler)
		{
			CommandBindings.Add(new CommandBinding(command, handler));
			InputBindings.Add(TextAreaDefaultInputHandler.CreateFrozenKeyBinding(command, modifiers, key));
		}
		
		static CaretNavigationCommandHandler()
		{
			const ModifierKeys None = ModifierKeys.None;
			const ModifierKeys Ctrl = ModifierKeys.Control;
			const ModifierKeys Shift = ModifierKeys.Shift;
			
			AddBinding(EditingCommands.MoveLeftByCharacter, None, Key.Left, OnMoveCaret(CaretMovementType.CharLeft));
			AddBinding(EditingCommands.SelectLeftByCharacter, Shift, Key.Left, OnMoveCaretExtendSelection(CaretMovementType.CharLeft));
			AddBinding(EditingCommands.MoveRightByCharacter, None, Key.Right, OnMoveCaret(CaretMovementType.CharRight));
			AddBinding(EditingCommands.SelectRightByCharacter, Shift, Key.Right, OnMoveCaretExtendSelection(CaretMovementType.CharRight));
			
			AddBinding(EditingCommands.MoveLeftByWord, Ctrl, Key.Left, OnMoveCaret(CaretMovementType.WordLeft));
			AddBinding(EditingCommands.SelectLeftByWord, Ctrl | Shift, Key.Left, OnMoveCaretExtendSelection(CaretMovementType.WordLeft));
			AddBinding(EditingCommands.MoveRightByWord, Ctrl, Key.Right, OnMoveCaret(CaretMovementType.WordRight));
			AddBinding(EditingCommands.SelectRightByWord, Ctrl | Shift, Key.Right, OnMoveCaretExtendSelection(CaretMovementType.WordRight));
			
			AddBinding(EditingCommands.MoveUpByLine, None, Key.Up, OnMoveCaret(CaretMovementType.LineUp));
			AddBinding(EditingCommands.SelectUpByLine, Shift, Key.Up, OnMoveCaretExtendSelection(CaretMovementType.LineUp));
			AddBinding(EditingCommands.MoveDownByLine, None, Key.Down, OnMoveCaret(CaretMovementType.LineDown));
			AddBinding(EditingCommands.SelectDownByLine, Shift, Key.Down, OnMoveCaretExtendSelection(CaretMovementType.LineDown));
			
			AddBinding(EditingCommands.MoveDownByPage, None, Key.PageDown, OnMoveCaret(CaretMovementType.PageDown));
			AddBinding(EditingCommands.SelectDownByPage, Shift, Key.PageDown, OnMoveCaretExtendSelection(CaretMovementType.PageDown));
			AddBinding(EditingCommands.MoveUpByPage, None, Key.PageUp, OnMoveCaret(CaretMovementType.PageUp));
			AddBinding(EditingCommands.SelectUpByPage, Shift, Key.PageUp, OnMoveCaretExtendSelection(CaretMovementType.PageUp));
			
			AddBinding(EditingCommands.MoveToLineStart, None, Key.Home, OnMoveCaret(CaretMovementType.LineStart));
			AddBinding(EditingCommands.SelectToLineStart, Shift, Key.Home, OnMoveCaretExtendSelection(CaretMovementType.LineStart));
			AddBinding(EditingCommands.MoveToLineEnd, None, Key.End, OnMoveCaret(CaretMovementType.LineEnd));
			AddBinding(EditingCommands.SelectToLineEnd, Shift, Key.End, OnMoveCaretExtendSelection(CaretMovementType.LineEnd));
			
			AddBinding(EditingCommands.MoveToDocumentStart, Ctrl, Key.Home, OnMoveCaret(CaretMovementType.DocumentStart));
			AddBinding(EditingCommands.SelectToDocumentStart, Ctrl | Shift, Key.Home, OnMoveCaretExtendSelection(CaretMovementType.DocumentStart));
			AddBinding(EditingCommands.MoveToDocumentEnd, Ctrl, Key.End, OnMoveCaret(CaretMovementType.DocumentEnd));
			AddBinding(EditingCommands.SelectToDocumentEnd, Ctrl | Shift, Key.End, OnMoveCaretExtendSelection(CaretMovementType.DocumentEnd));
			
			CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, OnSelectAll));
		}
		
		static void OnSelectAll(object target, ExecutedRoutedEventArgs args)
		{
			TextArea textArea = GetTextArea(target);
			if (textArea != null && textArea.Document != null) {
				args.Handled = true;
				textArea.Caret.Offset = textArea.Document.TextLength;
				textArea.Selection = new SimpleSelection(0, textArea.Document.TextLength);
			}
		}
		
		static TextArea GetTextArea(object target)
		{
			return target as TextArea;
		}
		
		static ExecutedRoutedEventHandler OnMoveCaret(CaretMovementType direction)
		{
            return (target, args) => {
                TextArea textArea = GetTextArea(target);
                if (textArea != null && textArea.Document != null)
                {
                    args.Handled = true;
                    CaretMover.MoveCaretClearSelection(textArea, direction);
                }
            };
        }

		static ExecutedRoutedEventHandler OnMoveCaretExtendSelection(CaretMovementType direction)
		{
			return (target, args) => {
				TextArea textArea = GetTextArea(target);
				if (textArea != null && textArea.Document != null) {
					args.Handled = true;
                    CaretMover.MoveCaretExtendSelection(textArea, direction);
				}
			};
		}
	}
}
