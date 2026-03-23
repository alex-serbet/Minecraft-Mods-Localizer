using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models.Utils;
using MinecraftLocalizer.Models.Services.Core;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace MinecraftLocalizer.Models.Services.Translation
{
    public sealed class FileService : IFileService
    {
        private readonly IDialogService _dialogService;
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public FileService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
        }

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
                _dialogService.ShowError($"Error while processing file {filePath}: {ex.Message}");
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




