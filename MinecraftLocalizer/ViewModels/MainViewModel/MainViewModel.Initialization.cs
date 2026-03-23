using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Models;
using MinecraftLocalizer.Models.Utils;
using System.Windows.Data;

namespace MinecraftLocalizer.ViewModels
{
    public partial class MainViewModel
    {
        private void InitializeCollections()
        {
            DataGridCollectionView = CollectionViewSource.GetDefaultView(LocalizationStrings);
            DataGridCollectionView.Filter = FilterDataGridEntries;

            _localizationDocumentStore.RawContentChanged += OnRawContentChanged;
            LocalizationStrings.CollectionChanged += (s, e) => UpdateDataGridLogoVisibility();

            TreeNodesCollectionView = CollectionViewSource.GetDefaultView(TreeNodes);
            TreeNodesCollectionView.Filter = FilterTreeViewEntries;

            TreeNodes.CollectionChanged += (s, e) => UpdateTreeNodesLogoVisibility();
        }

        private void InitializeCommands()
        {
            SaveTranslationCommand = new RelayCommand(SaveTranslation);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            RunTranslationCommand = new RelayCommand(async () => await RunTranslation());
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);
            OpenFileCommand = new RelayCommand(async () => await OpenFile());
            OpenResourcePackCommand = new RelayCommand(async () => await OpenResourcePack());
            OnTreeViewItemSelectedCommand = new RelayCommand<TreeNodeItem>(async node => await OnTreeViewItemSelectedAsync(node));
            SelectMissingTargetLocalesCommand = new RelayCommand(SelectNodesMissingTargetLocale);
            OpenSelectedNodeInExplorerCommand = new RelayCommand<TreeNodeItem>(OpenNodeInExplorer);
            CopySelectedCellCommand = new RelayCommand<System.Windows.Controls.DataGrid>(CopySelectedCell);
            OnApplicationExitCommand = new RelayCommand(OnApplicationExit);
            ToggleViewModeCommand = new RelayCommand(ToggleViewMode);
            CollapseConsoleCommand = new RelayCommand(CollapseConsole);
            ToggleConsoleOutputCommand = new RelayCommand(ToggleConsoleOutput);
        }

        private void InitializeModes()
        {
            Modes.AddRange(
            [
                new TranslationModeItem { ModeTitle = Properties.Resources.NotSelectedModeTitle, Type = TranslationModeType.NotSelected },
                new TranslationModeItem { ModeTitle = Properties.Resources.ModsModeTitle, Type = TranslationModeType.Mods },
                new TranslationModeItem { ModeTitle = Properties.Resources.OneFileModeTitle, Type = TranslationModeType.OneFile },
                new TranslationModeItem { ModeTitle = Properties.Resources.ResourcePackModeTitle, Type = TranslationModeType.ResourcePack },
                new TranslationModeItem { ModeTitle = "FTB Quests", Type = TranslationModeType.Quests },
                new TranslationModeItem { ModeTitle = "Patchouli", Type = TranslationModeType.Patchouli },
                new TranslationModeItem { ModeTitle = "Better Questing", Type = TranslationModeType.BetterQuesting },
            ]);

            SelectedMode = Modes.FirstOrDefault();
        }
    }
}






