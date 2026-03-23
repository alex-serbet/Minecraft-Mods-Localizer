using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models.Localization.Requests;
using MinecraftLocalizer.Models.Localization.TextProcessors;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Localization
{
    public partial class TranslationOrchestrator
    {
        private readonly LocalizationDocumentStore _localizationManager;
        private readonly IProgress<(int current, int total, double percentage)> _progress;
        private readonly Action<string>? _onStreamingChunkReceived;
        private readonly Action<string>? _onLogMessage;
        private readonly Action<string, IReadOnlyList<LocalizationItem>, string>? _onDocumentSnapshot;
        private readonly TranslationModeItem? _selectedMode;
        private readonly bool _useGpt4Free;
        private readonly HashSet<string> _patchouliSeededArchives = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _patchouliKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "name",
            "title",
            "text",
            "description"
        };

        private static readonly Dictionary<string, string[]> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            [".json"] = [".json"],
            [".lang"] = [".lang"],
            [".snbt"] = [".snbt"]
        };

        public static readonly Regex TranslationMarkerRegex =
            new(@"@(\d+)\s+(.+?)\s+\1@", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

        public static int GetBatchSize(bool useGpt4Free)
        {
            int configuredValue = useGpt4Free
                ? Properties.Settings.Default.Gpt4FreeBatchSize
                : Properties.Settings.Default.DeepSeekBatchSize;

            return Math.Clamp(configuredValue, 1, 500);
        }
        private static readonly ConcurrentDictionary<string, ILoadSource> LoadSourceCache = new();

        public TranslationOrchestrator(
            LocalizationDocumentStore localizationManager,
            IProgress<(int current, int total, double percentage)> progress,
            Action<string>? onStreamingChunkReceived = null,
            Action<string>? onLogMessage = null,
            Action<string, IReadOnlyList<LocalizationItem>, string>? onDocumentSnapshot = null,
            TranslationModeItem? selectedMode = null,
            bool useGpt4Free = false)
        {
            _localizationManager = localizationManager;
            _progress = progress;
            _onStreamingChunkReceived = onStreamingChunkReceived;
            _onLogMessage = onLogMessage;
            _onDocumentSnapshot = onDocumentSnapshot;
            _selectedMode = selectedMode;
            _useGpt4Free = useGpt4Free;
        }

        public async Task<bool> TranslateSelectedStrings(
            List<TreeNodeItem> selectedNodes,
            TranslationModeType modeType,
            CancellationToken cancellationToken,
            bool isTextBoxMode = false,
            string? currentDataGridFilePath = null,
            HashSet<string>? selectedEntryKeys = null)
        {
            if (selectedNodes.Count == 0)
                return false;

            Log($"Translation started. Mode: {modeType}. Nodes: {selectedNodes.Count}. Provider: {(_useGpt4Free ? "GPT4Free" : "DeepSeek")}.");

            return isTextBoxMode
                ? await ProcessTextBoxTranslation(cancellationToken)
                : await ProcessDataGridTranslation(
                    selectedNodes,
                    modeType,
                    cancellationToken,
                    currentDataGridFilePath,
                    selectedEntryKeys);
        }

        private async Task<bool> ProcessTextBoxTranslation(CancellationToken cancellationToken)
        {
            var processor = CreateTextProcessor();
            return await processor.ProcessAsync(cancellationToken);
        }

        private RegularTextProcessor CreateTextProcessor()
        {
            return new RegularTextProcessor(_localizationManager, _progress, _onStreamingChunkReceived, _onLogMessage, _useGpt4Free);
        }

        private async Task<bool> ProcessDataGridTranslation(
            List<TreeNodeItem> selectedNodes,
            TranslationModeType modeType,
            CancellationToken cancellationToken,
            string? currentDataGridFilePath,
            HashSet<string>? selectedEntryKeys)
        {
            var sourceLanguage = Properties.Settings.Default.SourceLanguage;
            var nodePairs = CollectTargetNodes(selectedNodes, sourceLanguage, modeType);

            if (nodePairs.Count == 0)
                return false;

            Log($"Source language: {sourceLanguage}. File pairs: {nodePairs.Count}.");

            int totalStrings = await CalculateTotalStringsAsync(
                nodePairs,
                modeType,
                currentDataGridFilePath,
                selectedEntryKeys);
            if (totalStrings == 0)
                return false;

            int totalTranslated = 0;
            _progress.Report((0, totalStrings, 0));
            Log($"Total strings to translate: {totalStrings}.");

            foreach (var (source, target) in nodePairs)
            {
                int translatedInThisFile = await ProcessNodePair(
                    source,
                    target,
                    modeType,
                    cancellationToken,
                    totalStrings,
                    totalTranslated,
                    currentDataGridFilePath,
                    selectedEntryKeys);

                totalTranslated += translatedInThisFile;
            }

            return true;
        }

        private async Task<int> ProcessNodePair(
            TreeNodeItem source,
            TreeNodeItem target,
            TranslationModeType modeType,
            CancellationToken cancellationToken,
            int totalStrings,
            int totalTranslatedSoFar,
            string? currentDataGridFilePath,
            HashSet<string>? selectedEntryKeys)
        {
            await UpdateUIAsync(() =>
            {
                target.IsTranslating = true;
                ExpandTranslationBranch(source, target.FilePath);
            });

            await LoadStringsAsync(source, target);
            Log($"Loaded strings. Target: {target.FilePath ?? target.FileName}. Total in file: {_localizationManager.LocalizationStrings.Count}.");

            if (modeType == TranslationModeType.Patchouli)
                ApplyPatchouliSelectionByJsonReference();

            ApplySelectionForCurrentFile(target.FilePath, currentDataGridFilePath, selectedEntryKeys);
            bool allowSelectAll = selectedEntryKeys == null || selectedEntryKeys.Count == 0;
            EnsureSelectionExistsOrSelectAll(modeType, allowSelectAll);

            var selectedStrings = _localizationManager.LocalizationStrings
                .Where(e => e.IsSelected)
                .ToList();

            if (selectedStrings.Count == 0 && !allowSelectAll)
            {
                Log($"Skipped file (no selected strings). Target: {target.FilePath ?? target.FileName}.");
                target.IsTranslating = false;
                return 0;
            }

            int translatedCount = 0;
            Log($"Translating file: {target.FilePath ?? target.FileName}. Selected strings: {selectedStrings.Count}. Batch size: {GetBatchSize(_useGpt4Free)}.");

            if (modeType == TranslationModeType.Patchouli)
            {
                TrySeedPatchouliArchive(source);
            }
            else
            {
                await SaveTranslations([target], modeType);
                Log($"Pre-saved file structure for: {target.FilePath ?? target.FileName}.");
                EmitDocumentSnapshot(target.FilePath);
            }

            while (selectedStrings.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int batchSize = GetBatchSize(_useGpt4Free);
                var batch = selectedStrings.Take(batchSize).ToList();
                Log($"Batch start. Size: {batch.Count}. Remaining in file: {selectedStrings.Count}.");
                translatedCount += await ProcessTranslationBatch(batch, cancellationToken);

                int currentTotalTranslated = totalTranslatedSoFar + translatedCount;
                _progress.Report((currentTotalTranslated, totalStrings,
                    (double)currentTotalTranslated / totalStrings * 100));

                await SaveTranslations([target], modeType);
                Log($"Saved file after batch: {target.FilePath ?? target.FileName}.");
                EmitDocumentSnapshot(target.FilePath);

                selectedStrings = _localizationManager.LocalizationStrings
                    .Where(e => e.IsSelected)
                    .ToList();
            }

            await SaveTranslations([target], modeType);
            Log($"Saved translations for file: {target.FilePath ?? target.FileName}. Translated strings: {translatedCount}.");
            EmitDocumentSnapshot(target.FilePath);
            await UpdateUIAsync(() =>
            {
                target.IsTranslating = false;
            });

            return translatedCount;
        }

        private void EmitDocumentSnapshot(string? filePath)
        {
            if (_onDocumentSnapshot == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            _onDocumentSnapshot(filePath, _localizationManager.LocalizationStrings, _localizationManager.RawContent);
        }

        private async Task<int> ProcessTranslationBatch(List<LocalizationItem> batch, CancellationToken cancellationToken)
        {
            var markedTexts = batch.Select((item, i) => ($"@{i} {item.OriginalString} {i}@", i)).ToList();
            var indexMap = markedTexts.ToDictionary(pair => pair.i, pair => batch[pair.i]);

            string combinedText = string.Join("\n", markedTexts.Select(pair => pair.Item1));

            string translatedText;
            using var translationRequest = new TranslationAiRequest(_useGpt4Free, _onLogMessage);
            while (true)
            {
                translatedText = await translationRequest.TranslateTextWithStreamingUIAsync(
                    combinedText,
                    cancellationToken,
                    chunk => _onStreamingChunkReceived?.Invoke(chunk));

                if (!string.IsNullOrWhiteSpace(translatedText))
                    break;

                // Don't skip the batch; wait and try again until we get a response.
                await Task.Delay(1000, cancellationToken);
            }

            var (translatedCount, _) = await ProcessTranslationResults(
                translatedText,
                indexMap,
                clearSelectionOnNoMatch: false);

            Log($"Batch result. Translated: {translatedCount}/{indexMap.Count}.");
            return translatedCount;
        }

        private async Task<(int translatedCount, bool anyMatchFound)> ProcessTranslationResults(
            string translatedText,
            Dictionary<int, LocalizationItem> indexMap,
            bool clearSelectionOnNoMatch)
        {
            int translatedCount = 0;
            bool anyMatchFound = false;
            int invalidCount = 0;

            foreach (var kvp in indexMap)
            {
                var entry = kvp.Value;
                if (string.IsNullOrWhiteSpace(entry.OriginalString))
                {
                    await UpdateUIAsync(() =>
                    {
                        entry.TranslatedString = string.Empty;
                        entry.IsSelected = false;
                    });
                    translatedCount++;
                }
            }

            foreach (Match match in TranslationMarkerRegex.Matches(translatedText))
            {
                if (!int.TryParse(match.Groups[1].Value, out int index) ||
                    !indexMap.TryGetValue(index, out var entry))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.OriginalString))
                    continue;

                string translatedContent = match.Groups[2].Value.Trim();
                bool isValid = IsValidTranslation(entry.OriginalString ?? string.Empty, translatedContent);

                await UpdateUIAsync(() =>
                {
                    entry.TranslatedString = isValid ? translatedContent : string.Empty;
                    // Keep invalid items selected so they can be retried in the next pass.
                    entry.IsSelected = !isValid;
                });

                if (isValid)
                    translatedCount++;
                else
                    invalidCount++;
                anyMatchFound = true;
            }

            if (!anyMatchFound)
            {
                if (clearSelectionOnNoMatch)
                {
                    await UpdateUIAsync(() =>
                    {
                        foreach (var item in indexMap.Values)
                        {
                            if (!string.IsNullOrWhiteSpace(item.OriginalString))
                                item.IsSelected = false;
                        }
                    });
                }
            }

            Log($"Batch parse. Valid: {translatedCount}, Invalid: {invalidCount}");
            return (translatedCount, anyMatchFound);
        }

        private void Log(string message)
        {
            _onLogMessage?.Invoke(message);
        }

        private void TrySeedPatchouliArchive(TreeNodeItem node)
        {
            if (string.IsNullOrWhiteSpace(node.ModPath))
                return;

            string seedKey = $"{node.ModPath}|{node.FilePath}";
            if (!_patchouliSeededArchives.Add(seedKey))
                return;

            LocalizationArchiveWriter.SeedPatchouliArchive(node);
            Log($"Pre-saved Patchouli structure for: {node.ModPath}.");
        }
    }
}


