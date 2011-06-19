using System;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
    public static class CaretMover
    {
        public static void MoveCaretClearSelection(TextArea textArea, CaretMovementType direction)
        {
            textArea.Selection = Selection.Empty;
            MoveCaret(textArea, direction);
            textArea.Caret.BringCaretToView();
        }

        public static void MoveCaretExtendSelection(TextArea textArea, CaretMovementType direction)
        {
            int oldOffset = textArea.Caret.Offset;
            MoveCaret(textArea, direction);
            textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldOffset, textArea.Caret.Offset);
            textArea.Caret.BringCaretToView();
        }

        #region Caret movement
        static void MoveCaret(TextArea textArea, CaretMovementType direction)
        {
            DocumentLine caretLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
            VisualLine visualLine = textArea.TextView.GetOrConstructVisualLine(caretLine);
            TextViewPosition caretPosition = textArea.Caret.Position;
            TextLine textLine = visualLine.GetTextLine(caretPosition.VisualColumn);
            switch (direction)
            {
                case CaretMovementType.CharLeft:
                    MoveCaretLeft(textArea, caretPosition, visualLine, CaretPositioningMode.Normal);
                    break;
                case CaretMovementType.CharRight:
                    MoveCaretRight(textArea, caretPosition, visualLine, CaretPositioningMode.Normal);
                    break;
                case CaretMovementType.WordLeft:
                    MoveCaretLeft(textArea, caretPosition, visualLine, CaretPositioningMode.WordStart);
                    break;
                case CaretMovementType.WordRight:
                    MoveCaretRight(textArea, caretPosition, visualLine, CaretPositioningMode.WordStart);
                    break;
                case CaretMovementType.LineUp:
                case CaretMovementType.LineDown:
                case CaretMovementType.PageUp:
                case CaretMovementType.PageDown:
                    MoveCaretUpDown(textArea, direction, visualLine, textLine, caretPosition.VisualColumn);
                    break;
                case CaretMovementType.DocumentStart:
                    SetCaretPosition(textArea, 0, 0);
                    break;
                case CaretMovementType.DocumentEnd:
                    SetCaretPosition(textArea, -1, textArea.Document.TextLength);
                    break;
                case CaretMovementType.LineStart:
                    MoveCaretToStartOfLine(textArea, visualLine);
                    break;
                case CaretMovementType.LineEnd:
                    MoveCaretToEndOfLine(textArea, visualLine);
                    break;
                default:
                    throw new NotSupportedException(direction.ToString());
            }
        }
        #endregion

        #region Home/End
        static void MoveCaretToStartOfLine(TextArea textArea, VisualLine visualLine)
        {
            int newVC = visualLine.GetNextCaretPosition(-1, LogicalDirection.Forward, CaretPositioningMode.WordStart);
            if (newVC < 0)
                throw ThrowUtil.NoValidCaretPosition();
            // when the caret is already at the start of the text, jump to start before whitespace
            if (newVC == textArea.Caret.VisualColumn)
                newVC = 0;
            int offset = visualLine.FirstDocumentLine.Offset + visualLine.GetRelativeOffset(newVC);
            SetCaretPosition(textArea, newVC, offset);
        }

        static void MoveCaretToEndOfLine(TextArea textArea, VisualLine visualLine)
        {
            int newVC = visualLine.VisualLength;
            int offset = visualLine.FirstDocumentLine.Offset + visualLine.GetRelativeOffset(newVC);
            SetCaretPosition(textArea, newVC, offset);
        }
        #endregion

        #region By-character / By-word movement
        static void MoveCaretRight(TextArea textArea, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode)
        {
            int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Forward, mode);
            if (pos >= 0)
            {
                SetCaretPosition(textArea, pos, visualLine.GetRelativeOffset(pos) + visualLine.FirstDocumentLine.Offset);
            }
            else
            {
                // move to start of next line
                DocumentLine nextDocumentLine = visualLine.LastDocumentLine.NextLine;
                if (nextDocumentLine != null)
                {
                    VisualLine nextLine = textArea.TextView.GetOrConstructVisualLine(nextDocumentLine);
                    pos = nextLine.GetNextCaretPosition(-1, LogicalDirection.Forward, mode);
                    if (pos < 0)
                        throw ThrowUtil.NoValidCaretPosition();
                    SetCaretPosition(textArea, pos, nextLine.GetRelativeOffset(pos) + nextLine.FirstDocumentLine.Offset);
                }
                else
                {
                    // at end of document
                    Debug.Assert(visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength == textArea.Document.TextLength);
                    SetCaretPosition(textArea, -1, textArea.Document.TextLength);
                }
            }
        }

        static void MoveCaretLeft(TextArea textArea, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode)
        {
            int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Backward, mode);
            if (pos >= 0)
            {
                SetCaretPosition(textArea, pos, visualLine.GetRelativeOffset(pos) + visualLine.FirstDocumentLine.Offset);
            }
            else
            {
                // move to end of previous line
                DocumentLine previousDocumentLine = visualLine.FirstDocumentLine.PreviousLine;
                if (previousDocumentLine != null)
                {
                    VisualLine previousLine = textArea.TextView.GetOrConstructVisualLine(previousDocumentLine);
                    pos = previousLine.GetNextCaretPosition(previousLine.VisualLength + 1, LogicalDirection.Backward, mode);
                    if (pos < 0)
                        throw ThrowUtil.NoValidCaretPosition();
                    SetCaretPosition(textArea, pos, previousLine.GetRelativeOffset(pos) + previousLine.FirstDocumentLine.Offset);
                }
                else
                {
                    // at start of document
                    Debug.Assert(visualLine.FirstDocumentLine.Offset == 0);
                    SetCaretPosition(textArea, 0, 0);
                }
            }
        }
        #endregion

        #region Line+Page up/down
        static void MoveCaretUpDown(TextArea textArea, CaretMovementType direction, VisualLine visualLine, TextLine textLine, int caretVisualColumn)
        {
            // moving up/down happens using the desired visual X position
            double xPos = textArea.Caret.DesiredXPos;
            if (double.IsNaN(xPos))
                xPos = textLine.GetDistanceFromCharacterHit(new CharacterHit(caretVisualColumn, 0));
            // now find the TextLine+VisualLine where the caret will end up in
            VisualLine targetVisualLine = visualLine;
            TextLine targetLine;
            int textLineIndex = visualLine.TextLines.IndexOf(textLine);
            switch (direction)
            {
                case CaretMovementType.LineUp:
                    {
                        // Move up: move to the previous TextLine in the same visual line
                        // or move to the last TextLine of the previous visual line
                        int prevLineNumber = visualLine.FirstDocumentLine.LineNumber - 1;
                        if (textLineIndex > 0)
                        {
                            targetLine = visualLine.TextLines[textLineIndex - 1];
                        }
                        else if (prevLineNumber >= 1)
                        {
                            DocumentLine prevLine = textArea.Document.GetLineByNumber(prevLineNumber);
                            targetVisualLine = textArea.TextView.GetOrConstructVisualLine(prevLine);
                            targetLine = targetVisualLine.TextLines[targetVisualLine.TextLines.Count - 1];
                        }
                        else
                        {
                            targetLine = null;
                        }
                        break;
                    }
                case CaretMovementType.LineDown:
                    {
                        // Move down: move to the next TextLine in the same visual line
                        // or move to the first TextLine of the next visual line
                        int nextLineNumber = visualLine.LastDocumentLine.LineNumber + 1;
                        if (textLineIndex < visualLine.TextLines.Count - 1)
                        {
                            targetLine = visualLine.TextLines[textLineIndex + 1];
                        }
                        else if (nextLineNumber <= textArea.Document.LineCount)
                        {
                            DocumentLine nextLine = textArea.Document.GetLineByNumber(nextLineNumber);
                            targetVisualLine = textArea.TextView.GetOrConstructVisualLine(nextLine);
                            targetLine = targetVisualLine.TextLines[0];
                        }
                        else
                        {
                            targetLine = null;
                        }
                        break;
                    }
                case CaretMovementType.PageUp:
                case CaretMovementType.PageDown:
                    {
                        // Page up/down: find the target line using its visual position
                        double yPos = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.LineMiddle);
                        if (direction == CaretMovementType.PageUp)
                            yPos -= textArea.TextView.RenderSize.Height;
                        else
                            yPos += textArea.TextView.RenderSize.Height;
                        DocumentLine newLine = textArea.TextView.GetDocumentLineByVisualTop(yPos);
                        targetVisualLine = textArea.TextView.GetOrConstructVisualLine(newLine);
                        targetLine = targetVisualLine.GetTextLineByVisualYPosition(yPos);
                        break;
                    }
                default:
                    throw new NotSupportedException(direction.ToString());
            }
            if (targetLine != null)
            {
                CharacterHit ch = targetLine.GetCharacterHitFromDistance(xPos);
                SetCaretPosition(textArea, targetVisualLine, targetLine, ch, false);
                textArea.Caret.DesiredXPos = xPos;
            }
        }
        #endregion

        #region SetCaretPosition
        static void SetCaretPosition(TextArea textArea, VisualLine targetVisualLine, TextLine targetLine,
                                     CharacterHit ch, bool allowWrapToNextLine)
        {
            int newVisualColumn = ch.FirstCharacterIndex + ch.TrailingLength;
            int targetLineStartCol = targetVisualLine.GetTextLineVisualStartColumn(targetLine);
            if (!allowWrapToNextLine && newVisualColumn >= targetLineStartCol + targetLine.Length)
                newVisualColumn = targetLineStartCol + targetLine.Length - 1;
            int newOffset = targetVisualLine.GetRelativeOffset(newVisualColumn) + targetVisualLine.FirstDocumentLine.Offset;
            SetCaretPosition(textArea, newVisualColumn, newOffset);
        }

        static void SetCaretPosition(TextArea textArea, int newVisualColumn, int newOffset)
        {
            textArea.Caret.Position = new TextViewPosition(textArea.Document.GetLocation(newOffset), newVisualColumn);
            textArea.Caret.DesiredXPos = double.NaN;
        }
        #endregion
    }
}
