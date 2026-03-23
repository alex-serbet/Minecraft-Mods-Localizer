using MinecraftLocalizer.Models;
using MinecraftLocalizer.Models.Localization.Requests;

namespace MinecraftLocalizer.Models.Localization.TextProcessors
{
    internal class RegularTextProcessor
    {
        private readonly LocalizationDocumentStore _localizationManager;
        private readonly IProgress<(int current, int total, double percentage)> _progress;
        private readonly Action<string>? _onStreamingChunkReceived;
        private readonly Action<string>? _onLogMessage;
        private readonly bool _useGpt4Free;

        public RegularTextProcessor(
            LocalizationDocumentStore manager,
            IProgress<(int current, int total, double percentage)> progress,
            Action<string>? onStreamingChunkReceived = null,
            Action<string>? onLogMessage = null,
            bool useGpt4Free = false)
        {
            _localizationManager = manager;
            _progress = progress;
            _onStreamingChunkReceived = onStreamingChunkReceived;
            _onLogMessage = onLogMessage;
            _useGpt4Free = useGpt4Free;
        }

        public async Task<bool> ProcessAsync(CancellationToken cancellationToken)
        {
            var lines = _localizationManager.RawContent.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length == 0)
                return false;

            int processedLines = 0;
            _progress.Report((0, lines.Length, 0));

            int batchSize = TranslationOrchestrator.GetBatchSize(_useGpt4Free);
            for (int i = 0; i < lines.Length; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = lines.Skip(i).Take(batchSize).ToArray();
                string translatedBatch = await ProcessBatch(batch, cancellationToken);

                UpdateLines(lines, i, batch, translatedBatch);
                processedLines += batch.Length;

                _progress.Report((processedLines, lines.Length,
                    (double)processedLines / lines.Length * 100));

                string currentTranslation = string.Join("\n", lines);
                _localizationManager.RawContent = currentTranslation;
                _onStreamingChunkReceived?.Invoke(currentTranslation);
            }

            return true;
        }

        private async Task<string> ProcessBatch(string[] batch, CancellationToken cancellationToken)
        {
            var markedText = batch.Select((text, idx) => $"@{idx} {text} {idx}@").ToArray();
            string combinedText = string.Join("\n", markedText);

            using var translationRequest = new TranslationAiRequest(_useGpt4Free, _onLogMessage);
            string translatedText = await translationRequest.TranslateTextWithStreamingUIAsync(
                combinedText,
                cancellationToken,
                chunk => _onStreamingChunkReceived?.Invoke(chunk));

            return ProcessTranslatedBatch(batch, translatedText);
        }

        private static string ProcessTranslatedBatch(string[] batch, string translatedText)
        {
            var translatedLines = new string[batch.Length];
            var matches = TranslationOrchestrator.TranslationMarkerRegex.Matches(translatedText);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int index) &&
                    index >= 0 &&
                    index < batch.Length)
                {
                    translatedLines[index] = match.Groups[2].Value.Trim();
                }
            }

            for (int i = 0; i < translatedLines.Length; i++)
            {
                if (string.IsNullOrEmpty(translatedLines[i]))
                {
                    translatedLines[i] = batch[i];
                }
            }

            return string.Join("\n", translatedLines);
        }

        private static void UpdateLines(string[] lines, int startIndex, string[] batch, string translatedBatch)
        {
            var translatedLines = translatedBatch.Split('\n');
            for (int j = 0; j < batch.Length && (startIndex + j) < lines.Length; j++)
            {
                lines[startIndex + j] = translatedLines[j];
            }
        }
    }
}






