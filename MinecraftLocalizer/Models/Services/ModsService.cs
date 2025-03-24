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
                DialogService.ShowError(Properties.Resources.ModsFilesMissingMessage);
                return [];
            }

            TreeViewNodes.Clear();

            LoadingWindow? loadingWindow = null;
            var cts = new CancellationTokenSource();

            loadingWindow = new LoadingWindow(Application.Current.MainWindow);
            loadingWindow.CancelRequested += (s, e) => cts.Cancel();
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
                        if (d.ModPath != null)
                            Application.Current.Dispatcher.Invoke(() =>
                                loadingWindow?.UpdateProgressMods(d.Progress, d.ModPath));
                    });

                    for (int processed = 0; processed < totalFiles; processed++)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        string modModPath = modFiles[processed];
                        string relativePath = Path.GetRelativePath(Properties.Settings.Default.DirectoryPath, modModPath);

                        var nodesFromModFile = await ProcessModFileAsync(modModPath);
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
            catch (OperationCanceledException)
            {
                return [];
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Loading error: {ex.Message}");
                return [];
            }
            finally
            {
                loadingWindow?.Close();
            }
        }

        private async Task<IEnumerable<TreeNodeItem>> ProcessModFileAsync(string modPath)
        {
            TreeNodeItem? modNode = null;

            try
            {
                using var archive = ZipFile.OpenRead(modPath);

                foreach (var entry in archive.Entries)
                {
                    var parts = entry.FullName.Split('/');
                    if (parts.Length == 4 && parts[0] == "assets" && parts[2] == "lang" && parts[3].EndsWith(".json"))
                    {
                        string modName = parts[1];
                        string fileName = parts[3];

                        // If the node has not been created yet, create it
                        modNode ??= await CreateRootNodeAsync(TreeViewNodes, modName, modPath);

                        // Check if the file exists in the child nodes
                        if (modNode.ChildrenNodes.FirstOrDefault(n => n.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            // Create a child node with the correct values
                            var childNode = new TreeNodeItem
                            {
                                FileName = fileName,
                                ModPath = modPath
                            };

                            modNode.ChildrenNodes.Add(childNode); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error while processing mod {modPath}: {ex.Message}");
            }

            return modNode != null ? [modNode] : Array.Empty<TreeNodeItem>();
        }

        private static async Task<TreeNodeItem> CreateRootNodeAsync(ObservableCollection<TreeNodeItem> parentCollection, string nodeTitle, string ModPath)
        {
            var newNode = new TreeNodeItem { FileName = nodeTitle, IsRoot = true, ModPath = ModPath };
            await Application.Current.Dispatcher.InvokeAsync(() => parentCollection.Add(newNode));
            return newNode;
        }
    }
}