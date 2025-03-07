namespace MinecraftLocalizer.Models.Utils
{
    public static class DebounceHelper
    {
        private const int SearchRefreshDelayMs = 300;

        private static Timer? _timer;
        public static void Debounce(Action action)
        {
            _timer?.Dispose();
            _timer = new Timer(_ => action(), null, SearchRefreshDelayMs, Timeout.Infinite);
        }
    }
}
