using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MinecraftLocalizer.Models.Ai.OpenRouter
{
    /// <summary>
    /// Fetches the list of available models from the OpenRouter API.
    /// </summary>
    public static class OpenRouterModelsApi
    {
        private const string ModelsUrl = "https://openrouter.ai/api/v1/models";

        public static async Task<IReadOnlyList<string>> ListModelsAsync(string apiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenRouter API key is empty.");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await client.GetAsync(ModelsUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var result = new List<string>();

            if (doc.RootElement.TryGetProperty("data", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.String)
                    {
                        string? id = idProp.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                            result.Add(id);
                    }
                }
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }
    }
}
