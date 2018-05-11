using System.Collections;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Hani.Utilities
{
    internal static class DragThumb
    {
        internal static bool IsVisible { get { return adorner.Visible; } }

        private static DragAdorner adorner;

        internal static void Set(Window owner)
        {
            adorner = new DragAdorner(owner);
            owner.PreviewDrop += new DragEventHandler(window_PreviewDrop);
        }

        private static void window_PreviewDrop(object sender, DragEventArgs e)
        {
            Hide();
        }

        internal static async void Show(ImageSource icon, int itemsCount)
        {
            adorner.Show(icon, itemsCount);
            while (adorner.Visible) { await Task.Delay(19); adorner.UpdatePosition(); }
        }

        internal static void Hide()
        {
            if (adorner.Visible) adorner.Hide();
        }
    }

    internal class DragAdorner : Adorner
    {
        internal bool Visible { get; private set; }

        private static Window Owner;
        private AdornerLayer adornerLayer;
        private static bool isRightToLeft;
        private static bool updating;
        private double left = 0.0;
        private double top = 0.0;

        private FrameworkElement content;
        private Image thumb;
        private Border borderCount;
        private Label labelCount;

        internal DragAdorner(Window owner)
            : base(owner.Content as UIElement)
        {
            IsHitTestVisible = false;

            Owner = owner;
            setContent();

            adornerLayer = AdornerLayer.GetAdornerLayer(owner.Content as Visual);
            adornerLayer.Add(this);

            base.AddLogicalChild(content);
            base.AddVisualChild(content);
        }

        private void setContent()
        {
            Grid grid = new Grid() { Width = 48, Height = 48, Visibility = Visibility.Collapsed };
            thumb = new Image() { Width = 48, Height = 48, Opacity = 0.7 };

            GradientStopCollection gsc = new GradientStopCollection();
            gsc.Add(new GradientStop(Colors.DeepSkyBlue, 0.0));
            gsc.Add(new GradientStop(Colors.DodgerBlue, 0.5));
            gsc.Add(new GradientStop(Colors.DeepSkyBlue, 1.0));
            LinearGradientBrush lgb = new LinearGradientBrush(gsc, new Point(0.5, 0), new Point(0.5, 1));

            borderCount = new Border() { Background = lgb, BorderThickness = new Thickness(1), BorderBrush = SolidColors.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom };
            labelCount = new Label() { Margin = new Thickness(4, 1, 4, 1), Padding = new Thickness(0), FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = SolidColors.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            borderCount.Child = labelCount;
            grid.Children.Add(thumb);
            grid.Children.Add(borderCount);
            content = grid;
        }

        internal void Show(ImageSource icon, int itemsCount)
        {
            isRightToLeft = (Owner.FlowDirection == FlowDirection.RightToLeft);

            thumb.Source = icon;

            if (itemsCount > 1)
            {
                labelCount.Content = itemsCount.String();
                borderCount.Visibility = Visibility.Visible;
            }
            else borderCount.Visibility = Visibility.Collapsed;

            UpdatePosition();

            this.content.Visibility = Visibility.Visible;

            Visible = true;
        }

        internal void Hide()
        {
            this.content.Visibility = Visibility.Collapsed;
            adornerLayer.Update(this.AdornedElement);
            Visible = false;
        }

        internal void UpdatePosition()
        {
            if (updating) return;
            updating = true;

            Point point = MouseUtilities.GetPosition(Owner);

            if ((point.X < 5.0) || (point.Y < 5.0) ||
            (point.X > (Owner.ActualWidth - 5.0)) ||
            (point.Y > (Owner.ActualHeight - 5.0))) Hide();

            top = point.Y - 40;
            if (isRightToLeft) left = Owner.ActualWidth - point.X - 25;
            else left = point.X - 25;

            adornerLayer.Update(this.AdornedElement);

            updating = false;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            content.Measure(constraint);
            return content.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            content.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return content;
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(left, top));
            return result;
        }

        private ArrayList children;

        protected override IEnumerator LogicalChildren
        {
            get
            {
                if (children == null)
                {
                    children = new ArrayList();
                    children.Add(this.content);
                }

                return children.GetEnumerator();
            }
        }
    }
}