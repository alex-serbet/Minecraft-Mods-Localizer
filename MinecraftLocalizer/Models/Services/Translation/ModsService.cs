using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Models.Utils;
using MinecraftLocalizer.Views;
using MinecraftLocalizer.Models.Services.Core;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace MinecraftLocalizer.Models.Services.Translation
{
    public class ModsService
    {
        private readonly IDialogService _dialogService;

        public ModsService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
        }
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public async Task<IEnumerable<TreeNodeItem>> LoadModsNodesAsync()
        {
            string modsDirectory = Path.Combine(Properties.Settings.Default.DirectoryPath, "mods");

            if (!Directory.Exists(modsDirectory))
            {
                _dialogService.ShowError(Properties.Resources.ModsFilesMissingMessage);
                return [];
            }

            TreeViewNodes.Clear();

            LoadingView? LoadingView = null;
            var cts = new CancellationTokenSource();

            LoadingView = new LoadingView(Application.Current.MainWindow);
            LoadingView.CancelRequested += (s, e) => cts.Cancel();
            LoadingView.Show();

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
                                LoadingView?.UpdateProgressMods(d.Progress, d.ModPath));
                    });

                    for (int processed = 0; processed < totalFiles; processed++)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        string modPath = modFiles[processed];
                        string relativePath = Path.GetRelativePath(Properties.Settings.Default.DirectoryPath, modPath);

                        var nodesFromModFile = await ProcessModFileAsync(modPath);
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
                _dialogService.ShowError($"Loading error: {ex.Message}");
                return [];
            }
            finally
            {
                LoadingView?.Close();
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
                    if (parts.Length == 4 && parts[0] == "assets" && parts[2] == "lang" && (parts[3].EndsWith(".json") || parts[3].EndsWith(".lang")))
                    {
                        string modName = parts[1];
                        string fileName = parts[3];

                        string fullFilePath = string.Join("/", parts);

                        // If the node has not been created yet, create it
                        modNode ??= await CreateRootNodeAsync(TreeViewNodes, modName, modPath);

                        // Check if the file exists in the child nodes
                        if (modNode.ChildrenNodes.FirstOrDefault(n => n.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            var childNode = new TreeNodeItem
                            {
                                FileName = fileName,
                                ModPath = modPath,
                                FilePath = fullFilePath,
                            };

                            modNode.ChildrenNodes.Add(childNode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error while processing mod {modPath}: {ex.Message}");
            }

            return modNode != null ? [modNode] : Array.Empty<TreeNodeItem>();
        }

        private static async Task<TreeNodeItem> CreateRootNodeAsync(ObservableCollection<TreeNodeItem> parentCollection, string nodeTitle, string modPath)
        {
            var newNode = new TreeNodeItem { FileName = nodeTitle, IsRoot = true, ModPath = modPath, FilePath = modPath };
            await Application.Current.Dispatcher.InvokeAsync(() => parentCollection.Add(newNode));
            return newNode;
        }
    }
}





