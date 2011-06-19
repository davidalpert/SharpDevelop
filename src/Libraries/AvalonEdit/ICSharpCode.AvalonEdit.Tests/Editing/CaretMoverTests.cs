using System;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Editing
{
    [TestFixture]
    public class CaretMoverTests
    {
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
    }
}
