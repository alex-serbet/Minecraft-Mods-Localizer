using MinecraftLocalizer.Models.Utils;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace MinecraftLocalizer.Models.Services
{
    class FileService
    {
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public async Task<IEnumerable<TreeNodeItem>> LoadFileNodesAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return [];

            try
            {
                IEnumerable<TreeNodeItem> nodes = [CreateFileNode(filePath)];

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TreeViewNodes.Clear();
                    TreeViewNodes.AddRange(nodes);
                });

                return nodes;
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error while processing file {filePath}: {ex.Message}");
                return [];
            }
        }

        private static TreeNodeItem CreateFileNode(string filePath)
        {
            return new TreeNodeItem
            {
                FileName = Path.GetFileName(filePath),
                ModPath = filePath,
                FilePath = filePath,
                IsRoot = true
            };
        }
    }
}
