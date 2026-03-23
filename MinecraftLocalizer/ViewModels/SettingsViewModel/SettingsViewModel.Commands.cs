using MinecraftLocalizer.Views;
using System.Globalization;
using System.Windows;
using System.Windows.Forms;

namespace MinecraftLocalizer.ViewModels
{
    public partial class SettingsViewModel
    {
        private void SaveSettings(object? parameter)
        {
            var oldDir = Properties.Settings.Default.DirectoryPath;
            bool isLanguageChanged = SelectedProgramLanguage != Properties.Settings.Default.ProgramLanguage;

            int count = PromptVariableRegex().Matches(Prompt).Count;
            if (count > 1)
            {
                _dialogService.ShowError(Properties.Resources.InvalidPromptMessage);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SelectedProviderId) && string.IsNullOrWhiteSpace(SelectedModelId))
            {
                _dialogService.ShowError(Properties.Resources.SelectModelForProviderMessage);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SelectedProviderId) && !IsModelSelectionEnabled && SelectedModelId != Properties.Resources.DialogServiceErrorTitle)
            {
                _dialogService.ShowError(Properties.Resources.SelectModelForProviderMessage);
                return;
            }

            Properties.Settings.Default.SourceLanguage = SelectedSourceLanguage;
            Properties.Settings.Default.TargetLanguage = SelectedTargetLanguage;
            Properties.Settings.Default.ProgramLanguage = SelectedProgramLanguage;
            Properties.Settings.Default.DirectoryPath = DirectoryPath;
            Properties.Settings.Default.Prompt = Prompt;
            Properties.Settings.Default.DeepSeekApiKey = DeepSeekApiKey;
            Properties.Settings.Default.Gpt4FreeApiKey = Gpt4FreeApiKey;
            Properties.Settings.Default.Gpt4FreeProviderId = SelectedProviderId;
            Properties.Settings.Default.Gpt4FreeModelId = SelectedModelId;
            Properties.Settings.Default.Gpt4FreeTemperature = Gpt4FreeTemperature;
            Properties.Settings.Default.DeepSeekTemperature = DeepSeekTemperature;
            Properties.Settings.Default.Gpt4FreeBatchSize = Gpt4FreeBatchSize;
            Properties.Settings.Default.DeepSeekBatchSize = DeepSeekBatchSize;
            Properties.Settings.Default.Save();

            var newCulture = new CultureInfo(Properties.Settings.Default.ProgramLanguage);
            Thread.CurrentThread.CurrentUICulture = newCulture;

            string message = Properties.Resources.SavedSettingsMessage;
            if (isLanguageChanged)
            {
                message += "\n" + Properties.Resources.RestartRequiredMessage;
            }

            _dialogService.ShowSuccess(message);

            bool directoryChanged = oldDir != Properties.Settings.Default.DirectoryPath;
            SettingsClosed?.Invoke(directoryChanged);
        }

        private void OpenAboutWindow()
        {
            _dialogService.ShowDialog<AboutView>(System.Windows.Application.Current.MainWindow, new AboutViewModel());
        }

        private void CloseWindowSettings(object? parameter)
        {
            if (parameter is Window settingsWindow)
            {
                settingsWindow.Close();
                SettingsClosed?.Invoke(false);
            }
        }

        private void SelectDirectoryPath(object? parameter)
        {
            using var folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                DirectoryPath = folderBrowserDialog.SelectedPath;
            }
        }

        private void ResetPrompt()
        {
            Prompt = Properties.Settings.Default.Properties["Prompt"].DefaultValue?.ToString() ?? string.Empty;
        }
    }
}
