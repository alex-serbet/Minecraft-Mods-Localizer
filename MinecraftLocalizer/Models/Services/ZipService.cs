using MinecraftLocalizer.Models.Utils;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace MinecraftLocalizer.Models.Services
{
    class ZipService
    {
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public async Task<IEnumerable<TreeNodeItem>> LoadZipNodesAsync(string archivePath)
        {
            if (string.IsNullOrEmpty(archivePath))
                return [];

            try
            {
                var zipNodes = await ProcessArchiveFileAsync(archivePath);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TreeViewNodes.Clear();
                    TreeViewNodes.AddRange(zipNodes);
                });
                return zipNodes;
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error while processing archive {archivePath}: {ex.Message}");
                return [];
            }
        }

        private static Task<IEnumerable<TreeNodeItem>> ProcessArchiveFileAsync(string archivePath)
        {
            var rootNode = new TreeNodeItem
            {
                FileName = Path.GetFileName(archivePath),
                IsRoot = true,
                ModPath = archivePath,
                FilePath = archivePath
            };

            try
            {
                using var archive = ZipFile.OpenRead(archivePath);
                var categoryNodes = new Dictionary<string, TreeNodeItem>();

                foreach (var entry in archive.Entries)
                {
                    if (IsSupportedFile(entry.FullName))
                    {
                        var parts = entry.FullName.Split('/')
                            .Where(p => !string.IsNullOrEmpty(p))
                            .ToArray();

                        if (parts.Length < 3) continue; // Пропускаем файлы не в категориях

                        var categoryName = parts[1]; // parts[0] и parts[1] игнорируем
                        var fileName = parts[^1];

                        if (!categoryNodes.TryGetValue(categoryName, out var categoryNode))
                        {
                            categoryNode = new TreeNodeItem
                            {
                                FileName = categoryName,
                                ModPath = archivePath,
                                FilePath = string.Join("/", parts.Take(3)) // Путь до категории
                            };
                            categoryNodes.Add(categoryName, categoryNode);
                            rootNode.ChildrenNodes.Add(categoryNode);
                        }

                        var fileNode = new TreeNodeItem
                        {
                            FileName = fileName,
                            ModPath = archivePath,
                            FilePath = entry.FullName
                        };
                        categoryNode.ChildrenNodes.Add(fileNode);
                    }
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error while processing archive {archivePath}: {ex.Message}");
            }

            return Task.FromResult<IEnumerable<TreeNodeItem>>([rootNode]);
        }

        private static bool IsSupportedFile(string fileName)
        {
            if (fileName.EndsWith('/')) return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension is ".json" or ".lang" or ".snbt";
        }
    }
}