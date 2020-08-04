using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RVVD.Merge_Text
{
    // Adorners must subclass the abstract base class Adorner. 
    public class DoubleArrowAdorner : Adorner
    {
        private PathGeometry _upArrow = null;
        private PathGeometry _downArrow = null;

        // Be sure to call the base class constructor. 
        public DoubleArrowAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            IsHitTestVisible = false;

            PathFigure myPathFigure = new PathFigure();
            myPathFigure.StartPoint = new Point(0, 0);

            LineSegment ls1 = new LineSegment(), ls2 = new LineSegment(), ls3 = new LineSegment();
            ls1.Point = new Point(15, 0);
            ls2.Point = new Point(7.5, 5);

            myPathFigure.IsClosed = true;

            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();
            myPathSegmentCollection.Add(ls1);
            myPathSegmentCollection.Add(ls2);
            myPathSegmentCollection.Add(ls3);

            myPathFigure.Segments = myPathSegmentCollection;

            PathFigureCollection myPathFigureCollection = new PathFigureCollection();
            myPathFigureCollection.Add(myPathFigure);

            _upArrow = new PathGeometry();
            _upArrow.Figures = myPathFigureCollection;
            _downArrow = new PathGeometry();
            _downArrow.Figures = myPathFigureCollection;
        }

        // A common way to implement an adorner's rendering behavior is to override the OnRender 
        // method, which is called by the layout system as part of a rendering pass. 
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect adornedElementRect = new Rect(this.AdornedElement.RenderSize);
            SolidColorBrush renderBrush = new SolidColorBrush(Colors.Green);
            renderBrush.Opacity = 0.2;
            Pen renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);
            Point midpoint = new Point(adornedElementRect.Width / 2, adornedElementRect.Height / 2);

            //down arrow
            TranslateTransform tt = new TranslateTransform();
            tt.X = midpoint.X - 7.5;
            tt.Y = 10;
            _downArrow.Transform = tt;
            drawingContext.DrawGeometry(renderBrush, renderPen, _downArrow);

            //up arrow
            MatrixTransform mt = new MatrixTransform();
            mt.Matrix = new Matrix(-1, 0, 0, -1, midpoint.X + 7.5, 6); //rotate 180 and offset
            _upArrow.Transform = mt;
            drawingContext.DrawGeometry(renderBrush, renderPen, _upArrow);
        }
    }
}
