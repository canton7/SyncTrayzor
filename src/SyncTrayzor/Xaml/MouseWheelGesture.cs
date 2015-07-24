using System.Windows.Input;

namespace SyncTrayzor.Xaml
{
    // Adapted from http://stackoverflow.com/a/7527482/1086121
    public class MouseWheelGesture : MouseGesture
    {
        private readonly WheelDirection direction;

        public static MouseWheelGesture CtrlDown => new MouseWheelGesture(ModifierKeys.Control, WheelDirection.Down);

        public static MouseWheelGesture CtrlUp =>  new MouseWheelGesture(ModifierKeys.Control, WheelDirection.Up);

        public MouseWheelGesture(ModifierKeys modifiers, WheelDirection direction)
            : base(MouseAction.WheelClick, modifiers)
        {
            this.direction = direction;
        }

        public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
        {
            if (!base.Matches(targetElement, inputEventArgs))
                return false;

            if (!(inputEventArgs is MouseWheelEventArgs))
                return false;

            var args = (MouseWheelEventArgs)inputEventArgs;

            switch (this.direction)
            {
                case WheelDirection.None:
                    return args.Delta == 0;
                case WheelDirection.Up:
                    return args.Delta > 0;
                case WheelDirection.Down:
                    return args.Delta < 0;
                default:
                    return false;
            }
        }

        public enum WheelDirection
        {
            None,
            Up,
            Down,
        }
    }
}
