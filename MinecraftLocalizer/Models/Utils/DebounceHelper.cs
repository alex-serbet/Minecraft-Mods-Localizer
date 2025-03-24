using System.Windows.Threading;

namespace MinecraftLocalizer.Models.Utils
{
    public static class DebounceHelper
    {
        private const int SearchRefreshDelayMs = 300;
        private static DispatcherTimer? _timer;

        public static void Debounce(Action action)
        {
            _timer?.Stop();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SearchRefreshDelayMs)
            };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                action();
            };
            _timer.Start();
        }
    }
}
