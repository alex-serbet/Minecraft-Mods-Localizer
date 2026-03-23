using MinecraftLocalizer.Models;
using System.Collections.ObjectModel;
using System.Windows;
using MinecraftLocalizer.Interfaces.Translation;

namespace MinecraftLocalizer.Models.Localization
{
    public class LocalizationDocumentStore
    {
        #region Fields and Properties
        private string _localizationText = string.Empty;

        public ObservableCollection<LocalizationItem> LocalizationStrings { get; } = [];
        public event EventHandler<string>? RawContentChanged;

        public string RawContent
        {
            get => _localizationText;
            set
            {
                if (_localizationText != value)
                {
                    _localizationText = value;
                    RawContentChanged?.Invoke(this, value);
                }
            }
        }
        #endregion

        #region Public Methods
        public async Task LoadStringsAsync(ILoadSource source)
        {
            var (items, rawContent) = await source.LoadAsync();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LocalizationStrings.Clear();
                RawContent = rawContent;

                foreach (var item in items)
                {
                    LocalizationStrings.Add(item);
                }

                UpdateRowNumbers();
            });
        }

        public async Task LoadFromCacheAsync(List<LocalizationItem> items, string rawContent)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LocalizationStrings.Clear();
                RawContent = rawContent;

                foreach (var item in items)
                {
                    LocalizationStrings.Add(item);
                }

                UpdateRowNumbers();
            });
        }
        #endregion

        #region Private Methods
        private void UpdateRowNumbers()
        {
            for (int i = 0; i < LocalizationStrings.Count; i++)
            {
                LocalizationStrings[i].RowNumber = i + 1;
            }
        }
        #endregion
    }
}










