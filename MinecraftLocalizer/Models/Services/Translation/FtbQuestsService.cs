using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Models.Services.Core;
using System.Collections.ObjectModel;
using System.IO;

namespace MinecraftLocalizer.Models.Services.Translation
{
    public class FtbQuestsService
    {
        private readonly IDialogService _dialogService;
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public FtbQuestsService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
        }

        public async Task<IEnumerable<TreeNodeItem>> LoadQuestsNodesAsync()
        {
            TreeViewNodes.Clear();

            var rootNode = new TreeNodeItem
            {
                FileName = "FTB Quests",
                FilePath = "",
                ModPath = "",
                ChildrenNodes = [],
                IsChecked = false,
                IsRoot = true
            };

            string[] directories =
            [
                Path.Combine(Properties.Settings.Default.DirectoryPath, "kubejs", "assets", "kubejs", "lang"),
                Path.Combine(Properties.Settings.Default.DirectoryPath, "config", "ftbquests", "quests")
            ];

            if (!directories.Any(Directory.Exists))
            {
                _dialogService.ShowError(Properties.Resources.FTBQuestsFilesMissingMessage);
                return [];
            }

            await Task.Run(() =>
            {
                foreach (var directory in directories.Where(Directory.Exists))
                {
                    foreach (var node in CreateNodesFromDirectory(directory))
                    {
                        rootNode.ChildrenNodes.Add(node);
                    }
                }
            });

            if (rootNode.ChildrenNodes.Count == 0)
            {
                _dialogService.ShowError(Properties.Resources.FTBQuestsFilesMissingMessage);
                return [];
            }

            TreeViewNodes.Add(rootNode);
            return [rootNode];
        }

        private static TreeNodeItem? CreateNodeFromFile(string modPath)
        {
            string fileName = Path.GetFileName(modPath);

            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return new TreeNodeItem
            {
                FileName = fileName,
                FilePath = modPath,
                ModPath = modPath,
                IsChecked = false,
                ChildrenNodes = []
            };
        }

        private static IEnumerable<TreeNodeItem> CreateNodesFromDirectory(string directoryPath)
        {
            foreach (var subDirectory in Directory.GetDirectories(directoryPath).OrderBy(Path.GetFileName))
            {
                var directoryNode = CreateDirectoryNode(subDirectory);
                if (directoryNode != null)
                {
                    yield return directoryNode;
                }
            }

            foreach (var filePath in Directory.GetFiles(directoryPath)
                         .Where(IsSupportedQuestFile)
                         .OrderBy(Path.GetFileName))
            {
                var fileNode = CreateNodeFromFile(filePath);
                if (fileNode != null)
                {
                    yield return fileNode;
                }
            }
        }

        private static TreeNodeItem? CreateDirectoryNode(string directoryPath)
        {
            var children = CreateNodesFromDirectory(directoryPath).ToList();
            if (children.Count == 0)
                return null;

            return new TreeNodeItem
            {
                FileName = Path.GetFileName(directoryPath),
                FilePath = directoryPath,
                ModPath = directoryPath,
                IsChecked = false,
                ChildrenNodes = [.. children]
            };
        }

        private static bool IsSupportedQuestFile(string filePath)
        {
            return filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase);
        }
    }
}




