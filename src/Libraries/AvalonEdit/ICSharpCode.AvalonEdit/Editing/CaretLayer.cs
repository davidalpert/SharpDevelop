// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Editing
{
    sealed class CaretLayer : Layer
    {
        CursorMode cursorMode;
        int offSet;
        bool isVisible;
        Rect caretRectangle;

        DispatcherTimer caretBlinkTimer = new DispatcherTimer();
        bool blink;

        public CaretLayer(TextView textView)
            : base(textView, KnownLayer.Caret)
        {
            this.IsHitTestVisible = false;
            caretBlinkTimer.Tick += new EventHandler(caretBlinkTimer_Tick);
            cursorMode = CursorMode.Insert; // the default
            offSet = 0; // default to the beginning
        }

        void caretBlinkTimer_Tick(object sender, EventArgs e)
        {
            blink = !blink;
            InvalidateVisual();
        }

        public void Show(Rect caretRectangle)
        {
            Show(caretRectangle);
        }

        public void Show(Rect caretRect, CursorMode mode, int cursorOffset)
        {
            this.caretRectangle = caretRectangle;
            this.isVisible = true;
            this.cursorMode = mode;
            this.offSet = cursorOffset;
            InvalidateVisual();
            StartBlinkAnimation();
        }

        public void Hide()
        {
            if (isVisible)
            {
                isVisible = false;
                StopBlinkAnimation();
                InvalidateVisual();
            }
        }

        void StartBlinkAnimation()
        {
            TimeSpan blinkTime = Win32.CaretBlinkTime;
            if (blinkTime.TotalMilliseconds >= 0)
            {
                blink = false;
                caretBlinkTimer_Tick(null, null);
                caretBlinkTimer.Interval = blinkTime;
                caretBlinkTimer.Start();
            }
        }

        void StopBlinkAnimation()
        {
            caretBlinkTimer.Stop();
        }

        internal Brush CaretBrush;
        internal Pen CaretPen;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (isVisible && blink)
            {
                switch (cursorMode)
                {
                    case CursorMode.Insert:
                        DrawInsertCursor(drawingContext);
                        break;
                    case CursorMode.Overwrite:
                        DrawOverwriteCursor(drawingContext);
                        break;
                }
            }
        }

        protected void DrawInsertCursor(DrawingContext drawingContext)
        {
            Brush caretBrush = this.CaretBrush;
            if (caretBrush == null)
                caretBrush = (Brush)textView.GetValue(TextBlock.ForegroundProperty);
            Rect r = new Rect(caretRectangle.X - textView.HorizontalOffset,
                              caretRectangle.Y - textView.VerticalOffset,
                              caretRectangle.Width,
                              caretRectangle.Height);
            drawingContext.DrawRectangle(caretBrush, null, PixelSnapHelpers.Round(r, PixelSnapHelpers.GetPixelSize(this)));
        }

        protected void DrawOverwriteCursor(DrawingContext drawingContext)
        {
            BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();

            builder.CornerRadius = 1;
            builder.AlignToMiddleOfPixels = true;

            var segment = new TextSegment() { StartOffset = this.offSet, Length = 1 };

            builder.AddSegment(textView, segment);
            builder.CloseFigure();

            Geometry geometry = builder.CreateGeometry();
            if (geometry != null)
            {
                drawingContext.DrawGeometry(this.CaretBrush, this.CaretPen, geometry);
            }
        }
    }
}
