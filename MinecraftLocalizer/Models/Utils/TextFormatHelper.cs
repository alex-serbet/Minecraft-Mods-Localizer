using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MinecraftLocalizer.Models.Utils
{
    public static class TextFormatHelper
    {
        public static readonly DependencyProperty TextSourceProperty =
            DependencyProperty.RegisterAttached(
                "TextSource",
                typeof(string),
                typeof(TextFormatHelper),
                new PropertyMetadata(null, OnTextSourceChanged));

        public static readonly DependencyProperty SearchTermProperty =
            DependencyProperty.RegisterAttached(
                "SearchTerm",
                typeof(string),
                typeof(TextFormatHelper),
                new PropertyMetadata(null, OnTextSourceChanged));

        public static readonly DependencyProperty HighlightColorProperty =
            DependencyProperty.RegisterAttached(
                "HighlightColor",
                typeof(Color),
                typeof(TextFormatHelper),
                new PropertyMetadata((Color)ColorConverter.ConvertFromString("#DD9144")));


        private static readonly Dictionary<string, Regex> RegexCache = [];

        public static void SetTextSource(DependencyObject element, string value) =>
            element.SetValue(TextSourceProperty, value);

        public static string GetTextSource(DependencyObject element) =>
            (string)element.GetValue(TextSourceProperty);

        public static void SetSearchTerm(DependencyObject element, string value) =>
            element.SetValue(SearchTermProperty, value);

        public static string GetSearchTerm(DependencyObject element) =>
            (string)element.GetValue(SearchTermProperty);

        public static void SetHighlightColor(DependencyObject element, Color value) =>
            element.SetValue(HighlightColorProperty, value);

        public static Color GetHighlightColor(DependencyObject element) =>
            (Color)element.GetValue(HighlightColorProperty);


        private static readonly object _updateLock = new();
        private static DateTime _lastUpdateTime = DateTime.MinValue;
        private static bool _isUpdatingRichTextBox = false;

        private static void OnTextSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (_isUpdatingRichTextBox)
                return;

            if (d is TextBlock textBlock)
                UpdateTextBlock(textBlock);
            else if (d is RichTextBox richTextBox)
                UpdateRichTextBox(richTextBox);
        }

        private static void UpdateTextBlock(TextBlock textBlock)
        {
            var text = GetTextSource(textBlock);
            var searchTerm = GetSearchTerm(textBlock);
            var color = GetHighlightColor(textBlock);
            var foreground = textBlock.Foreground;

            if (textBlock.Tag is CachedText cached &&
                cached.Text == text &&
                cached.SearchTerm == searchTerm)
            {
                return;
            }

            textBlock.Inlines.Clear();
            if (!string.IsNullOrEmpty(text))
            {
                var span = FormatText(text, searchTerm, color, foreground);
                textBlock.Inlines.AddRange(span.Inlines.ToList());
            }

            textBlock.Tag = new CachedText(text, searchTerm);
        }

        public static void UpdateRichTextBox(RichTextBox richTextBox)
        {
            lock (_updateLock)
            {
                if ((DateTime.Now - _lastUpdateTime).TotalMilliseconds < 100)
                    return;
                _lastUpdateTime = DateTime.Now;
            }
     
            richTextBox.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (_isUpdatingRichTextBox)
                        return;
                    _isUpdatingRichTextBox = true;

                    var text = GetTextSource(richTextBox);
                    var searchTerm = GetSearchTerm(richTextBox);
                    var color = GetHighlightColor(richTextBox);
                    var defaultForeground = richTextBox.Foreground;

                    if (string.IsNullOrEmpty(text))
                        return;

                    // Получаем текущий FlowDocument, если его нет — создаём новый
                    FlowDocument document = richTextBox.Document ?? new FlowDocument();
                    int caretOffset = GetCaretOffsetUsingText(document, richTextBox.CaretPosition);

                    // Берём первый параграф или создаём новый
                    if (document.Blocks.FirstBlock is not Paragraph paragraph)
                    {
                        paragraph = new Paragraph();
                        document.Blocks.Clear();
                        document.Blocks.Add(paragraph);
                    }
                    else
                    {
                        paragraph.Inlines.Clear();
                    }

                    var span = FormatText(text, searchTerm, color, defaultForeground);
                    foreach (var inline in span.Inlines.ToList())
                    {
                        paragraph.Inlines.Add(inline);
                    }

                    // Не переназначаем свойство Document, если оно уже установлено
                    // Восстанавливаем позицию каретки по вычисленному смещению
                    TextPointer newCaret = GetTextPointerAtOffsetUsingText(document, caretOffset) ?? document.ContentEnd;
                    richTextBox.CaretPosition = newCaret;
                    richTextBox.Focus();
                }
                finally
                {
                    _isUpdatingRichTextBox = false;
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Вычисляет смещение каретки относительно начала документа (по количеству символов).
        /// </summary>
        private static int GetCaretOffsetUsingText(FlowDocument document, TextPointer caretPosition)
        {
            TextRange range = new(document.ContentStart, caretPosition);
            return range.Text.Length;
        }

        /// <summary>
        /// Возвращает TextPointer в документе, соответствующий указанному смещению (по символам).
        /// </summary>
        private static TextPointer? GetTextPointerAtOffsetUsingText(FlowDocument document, int offset)
        {
            TextPointer pointer = document.ContentStart;
            int current = 0;
            while (pointer != null && current < offset)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    if (current + textRun.Length >= offset)
                    {
                        return pointer.GetPositionAtOffset(offset - current, LogicalDirection.Forward);
                    }
                    else
                    {
                        current += textRun.Length;
                        pointer = pointer.GetPositionAtOffset(textRun.Length, LogicalDirection.Forward);
                    }
                }
                else
                {
                    pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return pointer;
        }

        public static Span FormatText(string text, string searchTerm, Color highlightColor, Brush defaultForeground)
        {
            var span = new Span();
            if (string.IsNullOrEmpty(text))
                return span;

            if (string.IsNullOrEmpty(searchTerm))
            {
                span.Inlines.Add(new Run(text) { Foreground = defaultForeground });
                return span;
            }

            if (!RegexCache.TryGetValue(searchTerm, out var regex))
            {
                regex = new Regex(Regex.Escape(searchTerm), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                RegexCache[searchTerm] = regex;
            }

            int lastIndex = 0;
            foreach (Match match in regex.Matches(text))
            {
                if (match.Index > lastIndex)
                {
                    span.Inlines.Add(new Run(text[lastIndex..match.Index])
                    {
                        Foreground = defaultForeground
                    });
                }
                span.Inlines.Add(new Run(match.Value)
                {
                    Foreground = new SolidColorBrush(highlightColor),
                    FontWeight = FontWeights.Bold
                });

                // Добавляем пустой Run, чтобы разорвать наследование форматирования
                span.Inlines.Add(new Run("") { Foreground = defaultForeground });
                lastIndex = match.Index + match.Length;
            }
            if (lastIndex < text.Length)
            {
                span.Inlines.Add(new Run(text[lastIndex..])
                {
                    Foreground = defaultForeground
                });
            }

            return span;
        }

        private class CachedText(string text, string searchTerm)
        {
            public string Text { get; } = text;
            public string SearchTerm { get; } = searchTerm;
        }
    }
}