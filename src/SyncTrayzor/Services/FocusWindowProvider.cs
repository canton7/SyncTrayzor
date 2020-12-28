using System.Linq;
using System.Windows;

namespace SyncTrayzor.Services
{
    public interface IFocusWindowProvider
    {
        bool TryFocus<TViewModel>();
    }

    public class FocusWindowProvider : IFocusWindowProvider
    {
        private readonly Application application;

        public FocusWindowProvider(Application application)
        {
            this.application = application;
        }

        public bool TryFocus<TViewModel>()
        {
            var window = this.application.Windows.OfType<Window>().FirstOrDefault(x => x.DataContext is TViewModel);
            if (window != null)
            {
                window.Focus();
                return true;
            }
            return false;
        }
    }
}
