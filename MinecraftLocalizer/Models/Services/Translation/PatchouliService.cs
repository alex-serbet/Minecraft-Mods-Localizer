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
    public class PatchouliService
    {
        private readonly IDialogService _dialogService;

        public PatchouliService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
        }
        public ObservableCollection<TreeNodeItem> TreeViewNodes { get; private set; } = [];

        public async Task<IEnumerable<TreeNodeItem>> LoadPatchouliNodesAsync()
        {
            string modsDirectory = Path.Combine(Properties.Settings.Default.DirectoryPath, "mods");
            if (!Directory.Exists(modsDirectory))
            {
                _dialogService.ShowError(Properties.Resources.ModsFilesMissingMessage);
                return [];
            }

            TreeViewNodes.Clear();
            var patchouliNodes = new List<TreeNodeItem>();
            var modFiles = Directory.GetFiles(modsDirectory, "*.jar");

            var cts = new CancellationTokenSource();
            var LoadingView = new LoadingView(Application.Current.MainWindow);
            LoadingView.CancelRequested += (s, e) => cts.Cancel();
            LoadingView.Show();

            try
            {
                await Task.Run(async () =>
                {
                    var progress = new Progress<ProgressModsItem>(d =>
                        Application.Current.Dispatcher.Invoke(() =>
                            LoadingView?.UpdateProgressMods(d.Progress, d.ModPath)));

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

        private async Task<IEnumerable<TreeNodeItem>> ProcessPatchouliModFileAsync(string modPath)
        {
            TreeNodeItem? modNode = null;

            try
            {
                using var archive = ZipFile.OpenRead(modPath);
                foreach (var entry in archive.Entries)
                {
                    // Skip entries that represent directories (empty file name)
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var parts = entry.FullName.Split('/');
                    if (parts.Length < 5 || parts[0] != "assets" || parts[2] != "patchouli_books")
                        continue;

                    string modName = parts[1];
                    string langFolder = parts[4];
                    if (string.IsNullOrWhiteSpace(langFolder))
                        continue;

                    // Create the root node for the mod (if it doesn't exist)
                    modNode ??= await CreateRootNodeAsync(TreeViewNodes, modName, modPath);

                    // Create a language node under the root node
                    // Form the path for the language node: assets/modName/patchouli_books/bookName/langFolder
                    string langPath = string.Join("/", parts.Take(5));
                    var langNode = CreateChildNode(modNode.ChildrenNodes, langFolder, modPath, langPath);

                    if (parts[^1].EndsWith(".json"))
                    {
                        string fullFilePath = entry.FullName;
                        CreateChildNode(langNode.ChildrenNodes, entry.Name, modPath, fullFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error while processing Patchouli in {modPath}: {ex.Message}");
            }

            return modNode != null ? [modNode] : [];
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






