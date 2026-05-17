using Google.GenAI;
using Google.GenAI.Types;

namespace MinecraftLocalizer.Models.Ai.Gemini
{
    /// <summary>
    /// Lists Gemini models available for the supplied API key using the official SDK.
    /// </summary>
    public static class GeminiModelsApi
    {
        private const string ModelPrefix = "models/";

        public static async Task<IReadOnlyList<string>> ListModelsAsync(string apiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Gemini API key is empty.");

            var client = new Client(apiKey: apiKey);
            var result = new List<string>();

            var pager = await client.Models.ListAsync().ConfigureAwait(false);
            await foreach (var model in pager.WithCancellation(cancellationToken))
            {
                string name = model.Name ?? "";
                if (string.IsNullOrEmpty(name))
                    continue;

                string shortName = name.StartsWith(ModelPrefix, StringComparison.Ordinal)
                    ? name[ModelPrefix.Length..]
                    : name;

                result.Add(shortName);
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }
    }
}
