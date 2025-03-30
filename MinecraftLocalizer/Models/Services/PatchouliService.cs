using System.Collections.ObjectModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Windows;
using MinecraftLocalizer.Models.Utils;
using MinecraftLocalizer.Views;
using System.Diagnostics;

namespace MinecraftLocalizer.Models.Services
{
    public class PatchouliService
    {
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = new ObservableCollection<TreeNodeItem>();

        public async Task<IEnumerable<TreeNodeItem>> LoadPatchouliNodesAsync()
        {
            string modsDirectory = Path.Combine(Properties.Settings.Default.DirectoryPath, "mods");
            if (!Directory.Exists(modsDirectory))
            {
                DialogService.ShowError(Properties.Resources.ModsFilesMissingMessage);
                return Enumerable.Empty<TreeNodeItem>();
            }

            TreeViewNodes.Clear();
            var patchouliNodes = new List<TreeNodeItem>();
            var modFiles = Directory.GetFiles(modsDirectory, "*.jar");

            var cts = new CancellationTokenSource();
            var loadingWindow = new LoadingWindow(Application.Current.MainWindow);
            loadingWindow.CancelRequested += (s, e) => cts.Cancel();
            loadingWindow.Show();

            try
            {
                await Task.Run(async () =>
                {
                    var progress = new Progress<ProgressModsItem>(d =>
                        Application.Current.Dispatcher.Invoke(() =>
                            loadingWindow?.UpdateProgressMods(d.Progress, d.ModPath)));

                    for (int i = 0; i < modFiles.Length; i++)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        var modPath = modFiles[i];
                        string relativePath = Path.GetRelativePath(Properties.Settings.Default.DirectoryPath, modPath);

                        var nodesFromModFile = await ProcessPatchouliModFileAsync(modPath);
                        lock (patchouliNodes) patchouliNodes.AddRange(nodesFromModFile);

                        int percent = (int)((i + 1) * 100 / modFiles.Length);
                        ((IProgress<ProgressModsItem>)progress).Report(new ProgressModsItem(percent, relativePath));
                    }
                });

                TreeViewNodes.AddRange(patchouliNodes);
                return patchouliNodes;
            }
            catch (OperationCanceledException)
            {
                return Enumerable.Empty<TreeNodeItem>();
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Loading error: {ex.Message}");
                return Enumerable.Empty<TreeNodeItem>();
            }
            finally
            {
                loadingWindow?.Close();
            }
        }

        private async Task<IEnumerable<TreeNodeItem>> ProcessPatchouliModFileAsync(string modPath)
        {
            TreeNodeItem? modNode = null;

            try
            {
                using var archive = ZipFile.OpenRead(modPath);
                foreach (var entry in archive.Entries)
                {
                    // Пропускаем записи, представляющие каталоги (имя файла пустое)
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var parts = entry.FullName.Split('/');
                    // Проверяем, что запись находится в нужной папке
                    if (parts.Length < 5 || parts[0] != "assets" || parts[2] != "patchouli_books")
                        continue;

                    string modName = parts[1];
                    string bookName = parts[3];      // Хотя bookName больше не используется для построения дерева, он всё равно может быть частью пути
                    string langFolder = parts[4];
                    if (string.IsNullOrWhiteSpace(langFolder))
                        continue;

                    // Создаем корневой узел мода (если его нет)
                    modNode ??= await CreateRootNodeAsync(TreeViewNodes, modName, modPath);

                    // Создаем языковой узел под корневым узлом, игнорируя bookName
                    // Формируем путь для языкового узла: assets/modName/patchouli_books/bookName/langFolder
                    string langPath = string.Join("/", parts.Take(5));
                    var langNode = CreateChildNode(modNode.ChildrenNodes, langFolder, modPath, langPath);

                    // Для файла добавляем его как дочерний узел языкового узла, используя полный путь от "assets"
                    if (parts[^1].EndsWith(".json"))
                    {
                        string fullFilePath = entry.FullName;  // Полный путь от assets
                        CreateChildNode(langNode.ChildrenNodes, entry.Name, modPath, fullFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error while processing Patchouli in {modPath}: {ex.Message}");
            }

            return modNode != null ? new[] { modNode } : Enumerable.Empty<TreeNodeItem>();
        }

        private static TreeNodeItem CreateChildNode(ObservableCollection<TreeNodeItem> nodes, string name, string modPath, string filePath)
        {
            var node = nodes.FirstOrDefault(n => n.FileName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (node == null)
            {
                node = new TreeNodeItem
                {
                    FileName = name,
                    ModPath = modPath,
                    FilePath = filePath
                };

                nodes.Add(node);
            }

            return node;
        }

        private static async Task<TreeNodeItem> CreateRootNodeAsync(ObservableCollection<TreeNodeItem> parentCollection, string nodeTitle, string modPath)
        {
            var newNode = new TreeNodeItem
            {
                FileName = nodeTitle,
                IsRoot = true,
                ModPath = modPath,
                FilePath = modPath
            };

            await Application.Current.Dispatcher.InvokeAsync(() => parentCollection.Add(newNode));
            return newNode;
        }
    }
}
