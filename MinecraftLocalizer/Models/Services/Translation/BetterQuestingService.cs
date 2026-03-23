using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Models.Services.Core;
using System.Collections.ObjectModel;
using System.IO;

namespace MinecraftLocalizer.Models.Services.Translation
{
    public class BetterQuestingService
    {
        private readonly IDialogService _dialogService;
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public BetterQuestingService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
        }

        public async Task<IEnumerable<TreeNodeItem>> LoadQuestsNodesAsync()
        {
            TreeViewNodes.Clear();

            var rootNode = new TreeNodeItem
            {
                FileName = "Better Questing",
                FilePath = "",
                ModPath = "",
                ChildrenNodes = [],
                IsChecked = false,
                IsRoot = true
            };

            string[] directories =
            [
                Path.Combine(Properties.Settings.Default.DirectoryPath, "resources", "betterquesting", "lang")
            ];

            if (!directories.Any(Directory.Exists))
            {
                _dialogService.ShowError(Properties.Resources.BetterQuestingFilesMissingMessage);
                return [];
            }

            await Task.Run(() =>
            {
                foreach (var directory in directories.Where(Directory.Exists))
                {
                    var questFiles = Directory.GetFiles(directory)
                        .Where(file => file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                       file.EndsWith(".lang", StringComparison.OrdinalIgnoreCase));

                    foreach (var modPath in questFiles)
                    {
                        var node = CreateNodeFromFile(modPath);
                        if (node != null)
                        {
                            rootNode.ChildrenNodes.Add(node);
                        }
                    }
                }
            });

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
    }
}




