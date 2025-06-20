using GPili.Presentation.Popups;
using System.Diagnostics;

namespace GPili.Services
{
    public interface ILoaderService
    {
        Task ShowAsync(string message, bool isShowing);
    }

    public class LoaderService : ILoaderService
    {
        LoaderView? _current;
        Task? _popupTask; // Track the show task

        public async Task ShowAsync(string message, bool isShowing)
        {
            if (isShowing)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_current is not null)
                    {
                        _current.ViewModel.Message = message;
                    }
                    else
                    {
                        var popup = new LoaderView(message);
                        _current = popup;
                        // Start without awaiting
                        _popupTask = Shell.Current.ShowPopupAsync(popup);
                    }
                });
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_current is not null)
                    {
                        _current.Close(); // Now this can execute
                        _current = null;
                        _popupTask = null;
                    }
                });
            }
        }
    }
}
