namespace System.Windows.Media.Animation
{
    internal class GridLengthAnimation : AnimationTimeline
    {
        public static DependencyProperty FromProperty;
        public static DependencyProperty ToProperty;

        static GridLengthAnimation()
        {
            _set();
        }

        private static void _set()
        {
            FromProperty = DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));
            ToProperty = DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));
        }

        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }

        public override Type TargetPropertyType
        {
            get { return typeof(GridLength); }
        }

        public GridLength From
        {
            get { return (GridLength)GetValue(GridLengthAnimation.FromProperty); }
            set { SetValue(GridLengthAnimation.FromProperty, value); }
        }

        public GridLength To
        {
            get { return (GridLength)GetValue(GridLengthAnimation.ToProperty); }
            set { SetValue(GridLengthAnimation.ToProperty, value); }
        }

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            if (animationClock == null) throw new ArgumentNullException("animationClock", "animationClock cannot be a null reference");

            double fromValue = ((GridLength)GetValue(GridLengthAnimation.FromProperty)).Value;
            double toValue = ((GridLength)GetValue(GridLengthAnimation.ToProperty)).Value;

            if (fromValue > toValue) { return new GridLength((1 - animationClock.CurrentProgress.Value) * (fromValue - toValue) + toValue, this.To.IsStar ? GridUnitType.Star : GridUnitType.Pixel); }
            else { return new GridLength((animationClock.CurrentProgress.Value) * (toValue - fromValue) + fromValue, this.To.IsStar ? GridUnitType.Star : GridUnitType.Pixel); }
        }
    }
}