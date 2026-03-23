using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models.Utils;
using System.Collections.ObjectModel;
using MinecraftLocalizer.Models.Services.Core;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace MinecraftLocalizer.Models.Services.Translation
{
    public sealed class ZipService : IZipService
    {
        private readonly IDialogService _dialogService;
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public ZipService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
        }

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
                _dialogService.ShowError($"Error while processing archive {archivePath}: {ex.Message}");
                return [];
            }
        }

        private Task<IEnumerable<TreeNodeItem>> ProcessArchiveFileAsync(string archivePath)
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

                        if (parts.Length < 3)
                            continue;

                        var categoryName = parts[1];
                        var fileName = parts[^1];

                        if (!categoryNodes.TryGetValue(categoryName, out var categoryNode))
                        {
                            categoryNode = new TreeNodeItem
                            {
                                FileName = categoryName,
                                ModPath = archivePath,
                                FilePath = string.Join("/", parts.Take(3))
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
                _dialogService.ShowError($"Error while reading archive {archivePath}: {ex.Message}");
            }

            IEnumerable<TreeNodeItem> nodes = [rootNode];
            return Task.FromResult(nodes);
        }

        private static bool IsSupportedFile(string filePath)
        {
            return filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".lang", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase);
        }
    }
}




