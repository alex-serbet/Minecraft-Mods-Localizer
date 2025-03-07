using MinecraftLocalizer.Models.Utils;
using MinecraftLocalizer.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace MinecraftLocalizer.Models.Services
{
    public class ModsService
    {
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public async Task<IEnumerable<TreeNodeItem>> LoadModsNodesAsync()
        {
            string modsDirectory = Path.Combine(Properties.Settings.Default.DirectoryPath, "mods");

            if (!Directory.Exists(modsDirectory))
            {
                DialogService.ShowError(Properties.Resources.ModsFilesMissing);
                return [];
            }

            TreeViewNodes.Clear();

            LoadingWindow? loadingWindow = null;

            loadingWindow = new LoadingWindow(Application.Current.MainWindow);
            loadingWindow.Show();

            var modNodes = new List<TreeNodeItem>();

            try
            {
                var modFiles = Directory.GetFiles(modsDirectory, "*.jar");
                int totalFiles = modFiles.Length;

                await Task.Run(async () =>
                {
                    var progress = new Progress<ProgressModsItem>(d =>
                    {
                        if (d.FilePath != null)
                            Application.Current.Dispatcher.Invoke(() =>
                                loadingWindow?.UpdateProgressMods(d.Progress, d.FilePath));
                    });

                    for (int processed = 0; processed < totalFiles; processed++)
                    {
                        string modFilePath = modFiles[processed];
                        string relativePath = Path.GetRelativePath(Properties.Settings.Default.DirectoryPath, modFilePath);

                        var nodesFromModFile = await ProcessModFileAsync(modFilePath);
                        lock (modNodes)
                        {
                            modNodes.AddRange(nodesFromModFile);
                        }

                        int percent = (int)((processed + 1) / (double)totalFiles * 100);
                        ((IProgress<ProgressModsItem>)progress).Report(new ProgressModsItem(percent, relativePath));
                    }
                });

                TreeViewNodes.AddRange(modNodes);
                return modNodes;
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Ошибка загрузки: {ex.Message}");
                return [];
            }
            finally
            {
                loadingWindow?.Close();
            }
        }

        private async Task<IEnumerable<TreeNodeItem>> ProcessModFileAsync(string filePath)
        {
            TreeNodeItem? modNode = null;

            try
            {
                using var archive = ZipFile.OpenRead(filePath);

                foreach (var entry in archive.Entries)
                {
                    var parts = entry.FullName.Split('/');
                    if (parts.Length == 4 && parts[0] == "assets" && parts[2] == "lang" && parts[3].EndsWith(".json"))
                    {
                        string modName = parts[1];
                        string fileName = parts[3];

                        // If the node has not been created yet, create it
                        modNode ??= await CreateRootNodeAsync(TreeViewNodes, modName, filePath);

                        // Check if the file exists in the child nodes
                        if (modNode.ChildrenNodes.FirstOrDefault(n => n.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            // Create a child node with the correct values
                            var childNode = new TreeNodeItem
                            {
                                FileName = fileName,
                                FilePath = filePath,
                                ModName = modName
                            };

                            modNode.ChildrenNodes.Add(childNode); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Ошибка при обработке мода {filePath}: {ex.Message}");
            }

            return modNode != null ? [modNode] : Array.Empty<TreeNodeItem>();
        }

        private static async Task<TreeNodeItem> CreateRootNodeAsync(ObservableCollection<TreeNodeItem> parentCollection, string nodeTitle, string filePath)
        {
            var newNode = new TreeNodeItem { FileName = nodeTitle, IsRoot = true, FilePath = filePath };
            await Application.Current.Dispatcher.InvokeAsync(() => parentCollection.Add(newNode));
            return newNode;
        }
    }
}