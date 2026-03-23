using MinecraftLocalizer.Models;
using MinecraftLocalizer.Models.Utils;
using System.Windows;

namespace MinecraftLocalizer.ViewModels
{
    public partial class MainViewModel
    {
        private void RefreshDataGridSearch() =>
            DebounceHelper.Debounce(() => Application.Current.Dispatcher.Invoke(() => DataGridCollectionView?.Refresh()));

        private void RefreshTreeViewSearch() =>
            DebounceHelper.Debounce(() => Application.Current.Dispatcher.Invoke(() => TreeNodesCollectionView?.Refresh()));

        private bool FilterDataGridEntries(object item) =>
            item is LocalizationItem entry &&
            (entry.OriginalString?.Contains(SearchDataGridText, StringComparison.CurrentCultureIgnoreCase) == true ||
             entry.TranslatedString?.Contains(SearchDataGridText, StringComparison.CurrentCultureIgnoreCase) == true);

        private bool FilterTreeViewEntries(object item) =>
            item is TreeNodeItem node &&
            (node.FileName.Contains(SearchTreeViewText, StringComparison.CurrentCultureIgnoreCase) ||
             node.ChildrenNodes.Any(child => child.FileName.Contains(SearchTreeViewText, StringComparison.CurrentCultureIgnoreCase)));
    }
}




