using System;
using System.Linq;
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
        public static void MoveCaret(TextArea textArea, CaretMovementType direction)
        {
            MoveCaret(textArea, direction, 1);
        }

        public static void MoveCaret(TextArea textArea, CaretMovementType direction, bool multiline)
        {
            MoveCaret(textArea, direction, 1, multiline, false);
        }

        public static void MoveCaret(TextArea textArea, CaretMovementType direction, int numberOfTimes)
        {
            MoveCaret(textArea, direction, numberOfTimes, true, false);
        }

        public static void MoveCaret(TextArea textArea, CaretMovementType direction, int numberOfTimes, bool multiline, bool skipLastCharacter)
        {
            textArea.Selection = Selection.Empty;
            MoveTheCaret(textArea, direction, numberOfTimes, multiline, skipLastCharacter);
            textArea.Caret.BringCaretToView();
        }

        public static void MoveCaretExtendSelection(TextArea textArea, CaretMovementType direction)
        {
            MoveCaretExtendSelection(textArea, direction, 1);
        }

        public static void MoveCaretExtendSelection(TextArea textArea, CaretMovementType direction, int numberOfTimes)
        {
            MoveCaretExtendSelection(textArea, direction, numberOfTimes, true, false);
        }

        public static void MoveCaretExtendSelection(TextArea textArea, CaretMovementType direction, int numberOfTimes, bool multiline, bool skipLastCharacters)
        {
            int oldOffset = textArea.Caret.Offset;
            MoveTheCaret(textArea, direction, numberOfTimes, multiline, skipLastCharacters);
            textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldOffset, textArea.Caret.Offset);
            textArea.Caret.BringCaretToView();
        }

        #region Caret movement
        static VisualLine GetVisualLineFromCaretPosition(TextArea textArea)
        {
            DocumentLine caretLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
            return textArea.TextView.GetOrConstructVisualLine(caretLine);
        }

        static void MoveTheCaret(TextArea textArea, CaretMovementType direction, int numberOfTimes, bool multiline, bool skipLastCharacters)
        {
            VisualLine visualLine = GetVisualLineFromCaretPosition(textArea);
            TextViewPosition caretPosition = textArea.Caret.Position;
            TextLine textLine = visualLine.GetTextLine(caretPosition.VisualColumn);
            switch (direction)
            {
                case CaretMovementType.CharLeft:
                    MoveCaretLeft(textArea, caretPosition, visualLine, CaretPositioningMode.Normal, numberOfTimes, multiline, skipLastCharacters);
                    break;
                case CaretMovementType.CharRight:
                    MoveCaretRight(textArea, caretPosition, visualLine, CaretPositioningMode.Normal, numberOfTimes, multiline, skipLastCharacters);
                    break;
                case CaretMovementType.WordLeft:
                    MoveCaretLeft(textArea, caretPosition, visualLine, CaretPositioningMode.WordStart, numberOfTimes, true, true); // moving by words is always multiline
                    break;
                case CaretMovementType.WordRight:
                    MoveCaretRight(textArea, caretPosition, visualLine, CaretPositioningMode.WordStart, numberOfTimes, true, true); // moving by words is always multiline
                    break;
                case CaretMovementType.LineUp:
                case CaretMovementType.LineDown:
                case CaretMovementType.PageUp:
                case CaretMovementType.PageDown:
                    MoveCaretUpDown(textArea, direction, visualLine, textLine, caretPosition.VisualColumn, numberOfTimes, skipLastCharacters);
                    break;
                case CaretMovementType.DocumentStart:
                    SetCaretPosition(textArea, 0, 0);
                    break;
                case CaretMovementType.DocumentEnd:
                    SetCaretPosition(textArea, -1, textArea.Document.TextLength);
                    break;
                case CaretMovementType.LineStart:
                    MoveCaretToStartOfLine(textArea, visualLine, skipLastCharacters);
                    break;
                case CaretMovementType.LineEnd:
                    MoveCaretToEndOfLine(textArea, visualLine, skipLastCharacters);
                    break;
                default:
                    throw new NotSupportedException(direction.ToString());
            }
        }
        #endregion

        #region Home/End
        static void MoveCaretToStartOfLine(TextArea textArea, VisualLine visualLine, bool skipLastCharacter)
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

        static void MoveCaretToEndOfLine(TextArea textArea, VisualLine visualLine, bool skipLastCharacter)
        {
            int newVC = visualLine.VisualLength;
            if (skipLastCharacter)
            {
                newVC = newVC - 1;
            }
            int offset = visualLine.FirstDocumentLine.Offset + visualLine.GetRelativeOffset(newVC);
            SetCaretPosition(textArea, newVC, offset);
        }
        #endregion

        #region By-character / By-word movement
        static void MoveCaretRight(TextArea textArea, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, int numberOfTimes, bool multiline, bool skipLastCharacter)
        {
            for (var i = 0; i < numberOfTimes; i++)
            {
                int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Forward, mode);
                if (pos >= 0 && (!skipLastCharacter || pos < visualLine.VisualLength))
                {
                    SetCaretPosition(textArea, pos, visualLine.GetRelativeOffset(pos) + visualLine.FirstDocumentLine.Offset);
                }
                else if (multiline)
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

                // update our reference points for the next step
                visualLine = GetVisualLineFromCaretPosition(textArea);
                caretPosition = textArea.Caret.Position;
            }
        }

        static void MoveCaretLeft(TextArea textArea, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, int numberOfTimes, bool multiline, bool skipLastCharacter)
        {
            for (var i = 0; i < numberOfTimes; i++)
            {
                int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Backward, mode);
                if (pos >= 0)
                {
                    SetCaretPosition(textArea, pos, visualLine.GetRelativeOffset(pos) + visualLine.FirstDocumentLine.Offset);
                }
                else if (multiline)
                {
                    // move to end of previous line
                    DocumentLine previousDocumentLine = visualLine.FirstDocumentLine.PreviousLine;
                    if (previousDocumentLine != null)
                    {
                        VisualLine previousLine = textArea.TextView.GetOrConstructVisualLine(previousDocumentLine);
                        pos = previousLine.GetNextCaretPosition(previousLine.VisualLength + 1, LogicalDirection.Backward, mode);
                        if (pos < 0)
                            throw ThrowUtil.NoValidCaretPosition();
                        if (skipLastCharacter && pos == previousLine.VisualLength)
                            pos = pos - 1;
                        SetCaretPosition(textArea, pos, previousLine.GetRelativeOffset(pos) + previousLine.FirstDocumentLine.Offset);
                    }
                    else
                    {
                        // at start of document
                        Debug.Assert(visualLine.FirstDocumentLine.Offset == 0);
                        SetCaretPosition(textArea, 0, 0);
                    }
                }

                // update our reference points for the next step
                visualLine = GetVisualLineFromCaretPosition(textArea);
                caretPosition = textArea.Caret.Position;
            }
        }
        #endregion

        #region Line+Page up/down
        static void MoveCaretUpDown(TextArea textArea, CaretMovementType direction, VisualLine visualLine, TextLine textLine, int caretVisualColumn, int numberOfTimes, bool skipLastCharacter)
        {
            int numberOfLines = numberOfTimes;
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
                        int prevLineNumber = visualLine.FirstDocumentLine.LineNumber - numberOfLines;
                        if (textLineIndex > 0)
                        {
                            targetLine = visualLine.TextLines[textLineIndex - numberOfLines];
                        }
                        else if (prevLineNumber >= 1)
                        {
                            DocumentLine prevLine = textArea.Document.GetLineByNumber(prevLineNumber);
                            targetVisualLine = textArea.TextView.GetOrConstructVisualLine(prevLine);
                            targetLine = targetVisualLine.TextLines[targetVisualLine.TextLines.Count - 1];
                        }
                        else
                        {
                            DocumentLine prevLine = textArea.Document.Lines.FirstOrDefault();
                            if (prevLine != null)
                            {
                                targetVisualLine = textArea.TextView.GetOrConstructVisualLine(prevLine);
                                targetLine = targetVisualLine.TextLines[targetVisualLine.TextLines.Count - 1];
                            }
                            else
                            {
                                targetLine = null;
                            }
                        }
                        break;
                    }
                case CaretMovementType.LineDown:
                    {
                        // Move down: move to the next TextLine in the same visual line
                        // or move to the first TextLine of the next visual line
                        int nextLineNumber = visualLine.LastDocumentLine.LineNumber + numberOfLines;
                        if (textLineIndex < visualLine.TextLines.Count - 1)
                        {
                            targetLine = visualLine.TextLines[textLineIndex + numberOfLines];
                        }
                        else if (nextLineNumber <= textArea.Document.LineCount)
                        {
                            DocumentLine nextLine = textArea.Document.GetLineByNumber(nextLineNumber);
                            targetVisualLine = textArea.TextView.GetOrConstructVisualLine(nextLine);
                            targetLine = targetVisualLine.TextLines[0];
                        }
                        else
                        {
                            DocumentLine nextLine = textArea.Document.Lines.LastOrDefault();
                            if (nextLine != null)
                            {
                                targetVisualLine = textArea.TextView.GetOrConstructVisualLine(nextLine);
                                targetLine = targetVisualLine.TextLines[0];
                            }
                            else
                            {
                                targetLine = null;
                            }
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
                SetCaretPosition(textArea, targetVisualLine, targetLine, ch, false, skipLastCharacter);
                textArea.Caret.DesiredXPos = xPos;
            }
        }
        #endregion

        #region SetCaretPosition
        static void SetCaretPosition(TextArea textArea, VisualLine targetVisualLine, TextLine targetLine,
                                     CharacterHit ch, bool allowWrapToNextLine, bool doNotLandOnEndOfLine)
        {
            int newVisualColumn = ch.FirstCharacterIndex + ch.TrailingLength;
            int targetLineStartCol = targetVisualLine.GetTextLineVisualStartColumn(targetLine);
            if (!allowWrapToNextLine && newVisualColumn >= targetLineStartCol + targetLine.Length)
            {
                newVisualColumn = targetLineStartCol + targetLine.Length - 1;
            }
            if (newVisualColumn == targetLineStartCol + targetLine.Length - 1 && doNotLandOnEndOfLine)
            {
                newVisualColumn = newVisualColumn - 1;
            }
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
