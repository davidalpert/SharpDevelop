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
    public class ShowCursorArgs
    {
        public static ShowCursorArgs Default
        {
            get { return new ShowCursorArgs(CursorMode.Insert, 0, 0, 0); }
        }

        public ShowCursorArgs(CursorMode cursorMode, int offset, int currentColumn, int visualLength)
        {
            this.Mode = cursorMode;
            this.Offset = offset;
            this.AtTheEndOfAVisualLine = currentColumn > visualLength;
        }

        public CursorMode Mode { get; private set; }

        public int Offset { get; private set; }

        public bool AtTheEndOfAVisualLine { get; private set; }
    }

    public class DefaultCursorDrawingStrategy : ICursorDrawingStrategy
    {
        public void DrawCursor(DrawingContext drawingContext, DrawCursorArgs args)
        {
            if (drawingContext == null)
                throw new InvalidOperationException("Cannot draw a cursor into a null context");

            if (args == null)
                throw new InvalidOperationException("don't know what to draw with null args");

            switch (args.CursorMode)
            {
                case CursorMode.Insert:
                    DrawInsertCursor(drawingContext, args);
                    break;
                case CursorMode.Overwrite:
                    DrawOverwriteCursor(drawingContext, args);
                    break;
            }
        }

        private void DrawInsertCursor(DrawingContext drawingContext, DrawCursorArgs args)
        {
            Brush caretBrush = args.Brush;
            if (caretBrush == null)
                caretBrush = (Brush)(args.TextView.GetValue(TextBlock.ForegroundProperty));
            Rect r = new Rect(args.CaretRectangle.X - args.TextView.HorizontalOffset,
                              args.CaretRectangle.Y - args.TextView.VerticalOffset,
                              args.CaretRectangle.Width,
                              args.CaretRectangle.Height);
            drawingContext.DrawRectangle(caretBrush, null, PixelSnapHelpers.Round(r, PixelSnapHelpers.GetPixelSize(args.TextView)));
        }
        
        private void DrawOverwriteCursor(DrawingContext drawingContext, DrawCursorArgs args)
        {
            BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();

            builder.CornerRadius = 1;
            builder.AlignToMiddleOfPixels = true;

            var segment = new TextSegment() { StartOffset = args.Offset, Length = 1 };

            builder.AddSegment(args.TextView, segment);
            builder.CloseFigure();

            Geometry geometry = builder.CreateGeometry();
            if (geometry != null)
            {
                drawingContext.DrawGeometry(args.Brush, args.Pen, geometry);
            }
        }
    }

    public class DrawCursorArgs
    {
        public TextView TextView { get; set; }
        public CursorMode CursorMode { get; set; }
        public Rect CaretRectangle { get; set; }
        public Brush Brush { get; set; }
        public Pen Pen { get; set; }
        public int Offset { get; set; }
        public int Column { get; set; }
        public VisualLine VisualLine { get; set; }
    }

    public interface ICursorDrawingStrategy
    {
        void DrawCursor(DrawingContext drawingContext, DrawCursorArgs drawCursorArgs);
    }

    sealed class CaretLayer : Layer
    {
        DrawCursorArgs drawCursorArgs;
        bool isVisible;

        DispatcherTimer caretBlinkTimer = new DispatcherTimer();
        bool blink;

        public CaretLayer(TextView textView)
            : base(textView, KnownLayer.Caret)
        {
            this.IsHitTestVisible = false;
            caretBlinkTimer.Tick += new EventHandler(caretBlinkTimer_Tick);

            this.CursorDrawingStrategy = new DefaultCursorDrawingStrategy();
        }

        void caretBlinkTimer_Tick(object sender, EventArgs e)
        {
            blink = !blink;
            InvalidateVisual();
        }

        public void Show(Rect caretRectangle)
        {
            var args = new DrawCursorArgs
            {
                TextView = textView,
                Brush = CaretBrush,
                Pen = CaretPen,
                CaretRectangle = caretRectangle,
                CursorMode = CursorMode.Insert,
            };

            Show(args);
        }

        public void Show(DrawCursorArgs args)
        {
            this.drawCursorArgs = args;

            this.isVisible = true;
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
                drawCursorArgs.Brush = this.CaretBrush;
                drawCursorArgs.Pen = this.CaretPen;

                CursorDrawingStrategy.DrawCursor(drawingContext, drawCursorArgs);
            }
        }

        public ICursorDrawingStrategy CursorDrawingStrategy { get; set; }
    }
}
