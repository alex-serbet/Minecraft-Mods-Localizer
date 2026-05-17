using System.Collections.Concurrent;
using System.Text;

namespace MinecraftLocalizer.Models.Localization
{
    /// <summary>
    /// In-memory accumulator of "source -> translation" pairs collected across batches.
    /// Used to keep terminology stable across batches (the LLM has no memory between requests,
    /// so we feed it back its own earlier short translations as an "established translations" block
    /// in the system prompt of the next batch).
    /// <para>
    /// Only short, name-like entries are kept (items, blocks, entities, mechanics). Long descriptive
    /// sentences are ignored: they bloat the prompt and have no real reuse value.
    /// </para>
    /// </summary>
    public sealed class TranslationGlossary
    {
        private const int MaxWordsPerEntry = 3;
        private const int MaxCharsPerEntry = 40;
        private const int MaxEntriesPerBatch = 60;
        private const int MaxTotalEntries = 5000;

        private readonly ConcurrentDictionary<string, string> _entries = new(StringComparer.OrdinalIgnoreCase);

        public int Count => _entries.Count;

        public void TryAdd(string? source, string? translation)
        {
            if (!IsAcceptable(source) || !IsAcceptable(translation))
                return;

            // Identity translations (source == translation) are useless — they mean the LLM
            // did not actually translate the term. Storing them would lock the term forever.
            if (string.Equals(source!.Trim(), translation!.Trim(), StringComparison.OrdinalIgnoreCase))
                return;

            if (_entries.Count >= MaxTotalEntries)
                return;

            _entries.TryAdd(source.Trim(), translation.Trim());
        }

        public string BuildPromptBlockForBatch(IEnumerable<string> batchSourceLines)
        {
            if (_entries.IsEmpty)
                return string.Empty;

            string corpus = string.Join("\n", batchSourceLines);
            if (string.IsNullOrWhiteSpace(corpus))
                return string.Empty;

            var relevant = new List<KeyValuePair<string, string>>();
            foreach (var pair in _entries.ToArray())
            {
                if (corpus.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    relevant.Add(pair);
                    if (relevant.Count >= MaxEntriesPerBatch)
                        break;
                }
            }

            if (relevant.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("ESTABLISHED TRANSLATIONS (use these exact translations whenever the source term appears):");
            foreach (var pair in relevant)
                sb.AppendLine($"- \"{pair.Key}\" -> \"{pair.Value}\"");

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Loads glossary entries from a dictionary (e.g. deserialized from cache).
        /// </summary>
        public void LoadFrom(Dictionary<string, string>? dict)
        {
            if (dict == null) return;
            foreach (var (key, value) in dict)
                TryAdd(key, value);
        }

        /// <summary>
        /// Exports current entries as a plain dictionary (for serialization into cache).
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(_entries, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsAcceptable(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string trimmed = value.Trim();
            if (trimmed.Length > MaxCharsPerEntry) return false;

            if (trimmed.Contains('\n') || trimmed.Contains('\r')) return false;
            char last = trimmed[^1];
            if (last == '.' || last == '?' || last == '!' || last == ':' || last == ';') return false;

            int wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return wordCount is > 0 and <= MaxWordsPerEntry;
        }
    }
}
