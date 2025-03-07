using System.Collections.ObjectModel;
using System.IO;

namespace MinecraftLocalizer.Models.Services
{
    public class QuestsService
    {
        // Преобразуем коллекцию в правильный тип
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        // Сделаем метод асинхронным, но он не будет статическим
        public async Task<IEnumerable<TreeNodeItem>> LoadQuestsNodesAsync()
        {
            // Очищаем текущие узлы и работаем с данными экземпляра
            TreeViewNodes.Clear();

            var rootNode = new TreeNodeItem
            {
                FileName = "FTB Quests",
                FilePath = "",
                ChildrenNodes = [],
                IsChecked = false
            };

            string[] directories =
            [
                Path.Combine(Properties.Settings.Default.DirectoryPath, "kubejs", "assets", "kubejs", "lang"),
                Path.Combine(Properties.Settings.Default.DirectoryPath, "config", "ftbquests", "quests", "lang")
            ];

            if (!directories.Any(Directory.Exists))
            {
                DialogService.ShowError(Properties.Resources.QuestFilesMissing);
                return [];
            }

            await Task.Run(() =>
            {
                foreach (var directory in directories.Where(Directory.Exists))
                {
                    var questFiles = Directory.GetFiles(directory)
                        .Where(file => file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                       file.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase));

                    foreach (var filePath in questFiles)
                    {
                        var node = CreateNodeFromFile(filePath);
                        if (node != null)
                        {
                            rootNode.ChildrenNodes.Add(node);
                        }
                    }
                }
            });

            // Обновляем коллекцию экземпляра и возвращаем результат
            TreeViewNodes.Add(rootNode);
            return [rootNode];
        }

        // Метод создания узла из файла
        private static TreeNodeItem? CreateNodeFromFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return new TreeNodeItem
            {
                FileName = fileName,
                FilePath = filePath,
                IsChecked = false,
                ChildrenNodes = [] // ObservableCollection для дочерних узлов
            };
        }
        
    }
}
