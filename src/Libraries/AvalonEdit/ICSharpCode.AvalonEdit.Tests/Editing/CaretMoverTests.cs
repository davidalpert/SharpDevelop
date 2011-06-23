using System;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Editing
{
    [TestFixture]
    public class CaretMoverTests
    {
        #region Left/Right
        [Test]
        public void CaretMoverNavigatesLeft()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 3);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharLeft);

            Assert.AreEqual(new TextLocation(4, 2), textArea.Caret.Location);
        }

        [Test]
        public void WhenAtBeginningOfALine_MoveCaretCharLeft_MovesToEndOfPreviousLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3rd line\n4th line");
            textArea.Caret.Location = new TextLocation(4, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharLeft);

            Assert.AreEqual(new TextLocation(3, 9), textArea.Caret.Location);
        }

        [Test]
        public void WhenAtBeginningOfALine_MoveCaretCharLeft_WithoutMultiline_DoesNotGoToThePreviousLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3rd line\n4th line");
            textArea.Caret.Location = new TextLocation(4, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharLeft, false);

            Assert.AreEqual(new TextLocation(4, 1), textArea.Caret.Location);
        }

        [Test]
        public void WhenAtEndOfALine_MoveCaretCharRight_MovesToStartOfNextLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3rd line\n4th line");
            textArea.Caret.Location = new TextLocation(3, 9);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharRight);

            Assert.AreEqual(new TextLocation(4, 1), textArea.Caret.Location);
        }

        [Test]
        public void WhenAtEndOfALine_MoveCaretCharRight_WithoutMultiline_DoesNotGoToTheNextLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3rd line\n4th line");
            textArea.Caret.Location = new TextLocation(3, 9);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharRight, false);

            Assert.AreEqual(new TextLocation(3, 9), textArea.Caret.Location);
        }

        [Test]
        public void CaretMoverCanNavigateLeftMultipleTimes()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 3);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharLeft, 2);

            Assert.AreEqual(new TextLocation(4, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMoverCanNavigateRightMultipleTimes()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 3);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharRight, 2);

            Assert.AreEqual(new TextLocation(4, 5), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingLeftMultipleTimes_withoutMultiline_DoesNotMovePastBeginningOfLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 3);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharLeft, 5, false, false);

            Assert.AreEqual(new TextLocation(4, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingRightMultipleTimes_withoutMultiline_DoesNotMovePastEndOfLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 3);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharRight, 8, false, false);

            Assert.AreEqual(new TextLocation(4, 9), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingRightMultipleTimes_without_Multiline_and_SkippingTheLastCharacter_DoesNotLandOnEndOfLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(3, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharRight, 8, false, true);

            Assert.AreEqual(new TextLocation(3, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingLeftOffTheBeginningOfALine_with_Multiline_and_SkippingTheLastCharacter_DoesNotLandOnEndOfLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.CharLeft, 1, true, true);

            Assert.AreEqual(new TextLocation(3, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingUpToAShorterVisualLine_with_SkippingTheLastCharacter_DoesNotLandOnEndOfLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 5);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineUp, 1, false, true);

            Assert.AreEqual(new TextLocation(3, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingDownToAShorterVisualLine_with_SkippingTheLastCharacter_DoesNotLandOnEndOfLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3rd line is very long\n4th line");
            textArea.Caret.Location = new TextLocation(3, 19);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineDown, 1, false, true);

            Assert.AreEqual(new TextLocation(4, 8), textArea.Caret.Location);
        }

        #endregion

        #region Up/Down

        [Test]
        public void CaretMoverNavigatesDown()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(1, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineDown);

            Assert.AreEqual(new TextLocation(2, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMoverNavigatesUp()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineUp);

            Assert.AreEqual(new TextLocation(3, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMoverCanNavigateDownMultipleTimes()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(1, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineDown, 2);

            Assert.AreEqual(new TextLocation(3, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMoverCanNavigateUpMultipleTimes()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineUp, 2);

            Assert.AreEqual(new TextLocation(2, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingDownMultipleTimesDoesNotMovePastEndOfDocument()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(2, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineDown, 5);

            Assert.AreEqual(new TextLocation(4, 1), textArea.Caret.Location);
        }

        [Test]
        public void CaretMovingUpMultipleTimesDoesNotMovePastBeginningOfDocument()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(3, 1);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineUp, 5);

            Assert.AreEqual(new TextLocation(1, 1), textArea.Caret.Location);
        }

        #endregion

        [Test]
        public void CaretMoverNavigatesToEndOfLine()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 3);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineEnd);

            Assert.AreEqual(new TextLocation(4, 9), textArea.Caret.Location);
        }

        [Test]
        public void CaretMoverNavigatesToEndOfLine_excluding_last_character()
        {
            TextArea textArea = new TextArea();
            textArea.Document = new TextDocument("1\n2\n3\n4th line");
            textArea.Caret.Location = new TextLocation(4, 3);

            CaretMover.MoveCaret(textArea, CaretMovementType.LineEnd, 1, false, true);

            Assert.AreEqual(new TextLocation(4, 8), textArea.Caret.Location);
        }
    }
}
