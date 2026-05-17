namespace MinecraftLocalizer.Models.Localization.Requests
{
    /// <summary>
    /// Identifies which LLM backend handles a translation request.
    /// Stored in <c>Properties.Settings.Default.SelectedProvider</c> by its string name.
    /// </summary>
    public enum TranslationProvider
    {
        DeepSeek,
        Gpt4Free,
        Gemini,
    }

    public static class TranslationProviderParser
    {
        public static TranslationProvider FromSettings()
        {
            string raw = Properties.Settings.Default.SelectedProvider;
            return Enum.TryParse<TranslationProvider>(raw, ignoreCase: true, out var parsed)
                ? parsed
                : TranslationProvider.DeepSeek;
        }
    }
}
