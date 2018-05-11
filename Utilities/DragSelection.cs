using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Hani.Utilities
{
    internal class DragSelection : Adorner
    {
        internal bool Visible { get; private set; }

        private FrameworkElement ParentElement;
        private UIElement content;
        private AdornerLayer adornerLayer;
        private Point startPoint = new Point(0, 0);
        private bool isMouseDown;
        private string VisualHitName;

        private double MinX;
        private double MinY;

        private double width = 0.0;
        private double height = 0.0;

        private double offsetX;
        private double offsetY;

        private double VisualHeight = 0.0;
        private double LastHeight = 0.0;
        private double LastRow = 0.0;

        internal static DragSelection Register(Window parentWindow, FrameworkElement parentElement, double minX, double minY, string visualHitName)
        {
            return new DragSelection(parentWindow, parentElement, minX, minY, visualHitName);
        }

        internal DragSelection(Window parentWindow, FrameworkElement parentElement, double minX, double minY, string visualHitName)
            : base(parentElement)
        {
            ParentElement = parentElement;
            setAdornment();

            VisualHitName = visualHitName;
            offsetX = MinX = minX;
            offsetY = MinY = minY;
            IsHitTestVisible = false;

            parentElement.MouseDown += new MouseButtonEventHandler(ParentElement_MouseDown);

            parentWindow.MouseMove += new MouseEventHandler(parentWindow_MouseMove);
            parentWindow.MouseUp += new MouseButtonEventHandler(parentWindow_MouseUp);

            adornerLayer = AdornerLayer.GetAdornerLayer(parentElement);
            adornerLayer.Add(this);
        }

        private void setAdornment()
        {
            Border border = new Border() { Margin = new Thickness(2), BorderBrush = SolidColors.DeepSkyBlue, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(1) };
            Rectangle rectangle = new Rectangle() { Opacity = 0.5, Fill = SolidColors.DeepSkyBlue };

            border.Child = rectangle;

            border.Visibility = Visibility.Collapsed;
            content = border;
        }

        private void ParentElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((e.LeftButton == MouseButtonState.Pressed) || (e.RightButton == MouseButtonState.Pressed))
            {
                isMouseDown = true;
                startPoint = e.MouseDevice.GetPosition(ParentElement);
                Mouse.Capture(this);
            }
        }

        private void parentWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouseDown) return;

            if ((e.LeftButton == MouseButtonState.Pressed) || (e.RightButton == MouseButtonState.Pressed))
            {
                if (Visible) updatePosition(MouseUtilities.GetPosition(ParentElement));
                else show();
            }
            else hide();
        }

        private void parentWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMouseDown) hide();
        }

        private void show()
        {
            if (Visible) return;
            Visible = true;
            content.Visibility = Visibility.Visible;
        }

        private void hide()
        {
            if (!Visible) return;

            offsetX = 0.0;
            offsetY = 0.0;
            width = 0.0;
            height = 0.0;
            LastHeight = 0.0;
            LastRow = 0.0;

            content.Visibility = Visibility.Collapsed;
            adornerLayer.Update(this.AdornedElement);

            Mouse.Capture(null);
            isMouseDown = false;
            Visible = false;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            content.Arrange(new Rect(offsetX, offsetY, width, height));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return content;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            content.Measure(constraint);
            return content.DesiredSize;
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        private void updatePosition(Point point)
        {
            double x, y;

            x = (point.X > MinX) ? point.X : MinX;
            y = (point.Y > MinY) ? point.Y : MinY;

            if (x > startPoint.X)
            {
                if (x > ParentElement.ActualWidth) x = ParentElement.ActualWidth;
                width = x - startPoint.X;
                offsetX = startPoint.X;
            }
            else if (x < startPoint.X)
            {
                width = startPoint.X - x;
                offsetX = x;
            }

            if (y > startPoint.Y)
            {
                if (y > ParentElement.ActualHeight) y = ParentElement.ActualHeight;
                height = y - startPoint.Y;
                offsetY = startPoint.Y;
            }
            else if (y < startPoint.Y)
            {
                height = startPoint.Y - y;
                offsetY = y;
            }

            if ((VisualHeight == 0.0) && GetItemAt(y, true))
            {
                LastHeight = height;
                LastRow = offsetY;
            }
            else
            {
                if (height > (LastHeight + VisualHeight))
                {
                    if (GetItemAt(offsetY, true))
                    {
                        LastHeight = height;
                        LastRow = offsetY;
                    }
                }
                else if (height < (LastHeight - VisualHeight))
                {
                    if (GetItemAt(LastRow, false))
                    {
                        LastHeight = height;
                        LastRow = offsetY;
                    }
                }
            }

            adornerLayer.Update(this.AdornedElement);
        }

        private bool GetItemAt(double y, bool select)
        {
            HitTestResult result = VisualTreeHelper.HitTest(ParentElement, new Point(50.0, y));
            if (result != null)
            {
                DependencyObject dObj = VisualTreeHelper.GetParent(result.VisualHit);
                while (dObj != null)
                {
                    if (dObj.DependencyObjectType.Name == VisualHitName)
                    {
                        (dObj as ListBoxItem).IsSelected = select;
                        if (VisualHeight == 0.0) VisualHeight = (dObj as ListBoxItem).ActualHeight / 2;
                        return true;
                    }
                    dObj = VisualTreeHelper.GetParent(dObj);
                }
            }

            return false;
        }
    }
}