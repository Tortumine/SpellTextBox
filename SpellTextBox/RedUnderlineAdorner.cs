using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpellTextBox
{
    public class RedUnderlineAdorner : Adorner
    {
        SizeChangedEventHandler sizeChangedEventHandler;
        RoutedEventHandler routedEventHandler;
        ScrollChangedEventHandler scrollChangedEventHandler;

        public RedUnderlineAdorner(SpellTextBox textbox) : base(textbox)
        {


            sizeChangedEventHandler = new SizeChangedEventHandler(
                delegate 
                {
                    SignalInvalidate();
                });

            routedEventHandler = new RoutedEventHandler(
                delegate
                {
                    SignalInvalidate();
                });

            scrollChangedEventHandler = new ScrollChangedEventHandler(
                delegate 
                {
                    SignalInvalidate();
                });

            textbox.SizeChanged += sizeChangedEventHandler;

            textbox.SpellcheckCompleted += routedEventHandler;

            textbox.AddHandler(ScrollViewer.ScrollChangedEvent, scrollChangedEventHandler);
        }

        SpellTextBox box;
        Pen pen = CreateErrorPen();

        public void Dispose()
        {
            if (box != null)
            {
                box.SizeChanged -= sizeChangedEventHandler;
                box.SpellcheckCompleted -= routedEventHandler;
                box.RemoveHandler(ScrollViewer.ScrollChangedEvent, scrollChangedEventHandler);
            }
        }

        void SignalInvalidate()
        {
            box = (SpellTextBox)this.AdornedElement;
            box.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)InvalidateVisual);
        }

        DependencyObject GetTopLevelControl(DependencyObject control)
        {
            DependencyObject tmp = control;
            DependencyObject parent = null;
            while ((tmp = VisualTreeHelper.GetParent(tmp)) != null)
            {
                parent = tmp;
            }
            return parent;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (box != null && box.IsSpellCheckEnabled && box.IsSpellcheckCompleted)
            {
                int startLineIndex = box.GetFirstVisibleLineIndex();
                int endLineIndex = box.GetLastVisibleLineIndex();

                for (var i = 0; i < box.Checker.MisspelledWords.Count; i++)
                {
                    Word word = box.Checker.MisspelledWords[i];
                    if (word.LineIndex < startLineIndex)
                        continue;
                    if (word.LineIndex > endLineIndex)
                        break;

                    Rect rectangleBounds = new Rect();
                    rectangleBounds = box.TransformToVisual(GetTopLevelControl(box) as Visual).TransformBounds(LayoutInformation.GetLayoutSlot(box));

                    Rect startRect = box.GetRectFromCharacterIndex((Math.Min(box.GetCharacterIndexFromLineIndex(word.LineIndex) + word.Index, box.Text.Length)));
                    Rect endRect = box.GetRectFromCharacterIndex(Math.Min(box.GetCharacterIndexFromLineIndex(word.LineIndex) + word.Index + word.Length, box.Text.Length));
                    Rect startRectM = box.GetRectFromCharacterIndex((Math.Min(box.GetCharacterIndexFromLineIndex(word.LineIndex) + word.Index, box.Text.Length)));
                    Rect endRectM = box.GetRectFromCharacterIndex(Math.Min(box.GetCharacterIndexFromLineIndex(word.LineIndex) +  word.Index + word.Length, box.Text.Length));

                    startRectM.X += rectangleBounds.X;
                    startRectM.Y += rectangleBounds.Y;
                    endRectM.X += rectangleBounds.X;
                    endRectM.Y += rectangleBounds.Y;

                    if (rectangleBounds.Contains(startRectM) && rectangleBounds.Contains(endRectM))
                        drawingContext.DrawLine(pen, startRect.BottomLeft, endRect.BottomRight);
                }
            }
        }

        private static Pen CreateErrorPen()
        {
            double size = 4.0;

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(0.0, 0.0), false, false);
                context.PolyLineTo(new[] {
                    new Point(size * 0.25, size * 0.25),
                    new Point(size * 0.5, 0.0),
                    new Point(size * 0.75, size * 0.25),
                    new Point(size, 0.0)
                }, true, true);
            }

            var brushPattern = new GeometryDrawing
            {
                Pen = new Pen(Brushes.Red, 0.2),
                Geometry = geometry
            };

            var brush = new DrawingBrush(brushPattern)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0.0, size * 0.33, size * 3.0, size),
                ViewportUnits = BrushMappingMode.Absolute
            };

            var pen = new Pen(brush, size);
            pen.Freeze();

            return pen;
        }
    }
}
