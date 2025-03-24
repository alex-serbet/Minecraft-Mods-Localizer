using System.Collections.ObjectModel;
using System.IO;

namespace MinecraftLocalizer.Models.Services
{
    public class QuestsService
    {
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public async Task<IEnumerable<TreeNodeItem>> LoadQuestsNodesAsync()
        {
            TreeViewNodes.Clear();

            var rootNode = new TreeNodeItem
            {
                FileName = "FTB Quests",
                ModPath = "",
                ChildrenNodes = [],
                IsChecked = false,
                IsRoot = true
            };

            string[] directories =
            [
                Path.Combine(Properties.Settings.Default.DirectoryPath, "kubejs", "assets", "kubejs", "lang"),
                Path.Combine(Properties.Settings.Default.DirectoryPath, "config", "ftbquests", "quests", "lang")
            ];

            if (!directories.Any(Directory.Exists))
            {
                DialogService.ShowError(Properties.Resources.QuestFilesMissingMessage);
                return [];
            }

            await Task.Run(() =>
            {
                foreach (var directory in directories.Where(Directory.Exists))
                {
                    var questFiles = Directory.GetFiles(directory)
                        .Where(file => file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                       file.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase));

                    foreach (var ModPath in questFiles)
                    {
                        var node = CreateNodeFromFile(ModPath);
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

        private static TreeNodeItem? CreateNodeFromFile(string ModPath)
        {
            string fileName = Path.GetFileName(ModPath);

            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return new TreeNodeItem
            {
                FileName = fileName,
                ModPath = ModPath,
                IsChecked = false,
                ChildrenNodes = []
            };
        }
    }
}
