using System.Globalization;

namespace MinecraftLocalizer.ViewModels
{
    public partial class SettingsViewModel
    {
        private async void ResetToDefault()
        {
            if (_isResettingToDefault)
            {
                return;
            }

            _isResettingToDefault = true;
            try
            {
                await ResetToDefaultAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"{Properties.Resources.FailedToResetSettingsMessage}: {ex.Message}");
            }
            finally
            {
                _isResettingToDefault = false;
            }
        }

        private async Task ResetToDefaultAsync()
        {
            var settings = Properties.Settings.Default;

            string defaultSourceLanguage = GetDefaultSettingValue(nameof(settings.SourceLanguage), settings.SourceLanguage);
            string defaultTargetLanguage = GetDefaultSettingValue(nameof(settings.TargetLanguage), settings.TargetLanguage);
            string defaultProgramLanguage = GetDefaultSettingValue(nameof(settings.ProgramLanguage), settings.ProgramLanguage);
            string defaultDirectoryPath = GetDefaultSettingValue(nameof(settings.DirectoryPath), settings.DirectoryPath);
            string defaultPrompt = GetDefaultSettingValue(nameof(settings.Prompt), settings.Prompt);
            string defaultDeepSeekApiKey = GetDefaultSettingValue(nameof(settings.DeepSeekApiKey), settings.DeepSeekApiKey);
            string defaultGpt4FreeApiKey = GetDefaultSettingValue(nameof(settings.Gpt4FreeApiKey), settings.Gpt4FreeApiKey);
            double defaultGpt4FreeTemperature = GetDefaultSettingValue(nameof(settings.Gpt4FreeTemperature), settings.Gpt4FreeTemperature);
            double defaultDeepSeekTemperature = GetDefaultSettingValue(nameof(settings.DeepSeekTemperature), settings.DeepSeekTemperature);
            int defaultGpt4FreeBatchSize = GetDefaultSettingValue(nameof(settings.Gpt4FreeBatchSize), settings.Gpt4FreeBatchSize);
            int defaultDeepSeekBatchSize = GetDefaultSettingValue(nameof(settings.DeepSeekBatchSize), settings.DeepSeekBatchSize);
            int defaultLegacyBatchSize = GetDefaultSettingValue(nameof(settings.TranslationBatchSize), settings.TranslationBatchSize);
            string defaultProviderId = GetDefaultSettingValue(nameof(settings.Gpt4FreeProviderId), settings.Gpt4FreeProviderId);
            string defaultModelId = GetDefaultSettingValue(nameof(settings.Gpt4FreeModelId), settings.Gpt4FreeModelId);

            SelectedSourceLanguage = defaultSourceLanguage;
            SelectedTargetLanguage = defaultTargetLanguage;
            SelectedProgramLanguage = defaultProgramLanguage;
            DirectoryPath = defaultDirectoryPath;
            Prompt = defaultPrompt;
            DeepSeekApiKey = defaultDeepSeekApiKey;
            Gpt4FreeApiKey = defaultGpt4FreeApiKey;
            Gpt4FreeTemperature = defaultGpt4FreeTemperature;
            DeepSeekTemperature = defaultDeepSeekTemperature;
            Gpt4FreeBatchSize = defaultGpt4FreeBatchSize;
            DeepSeekBatchSize = defaultDeepSeekBatchSize;

            ProviderLoadError = string.Empty;
            ModelLoadError = string.Empty;
            _suppressProviderSelectionChangeLoad = true;
            SelectedProviderId = defaultProviderId;
            SelectedModelId = defaultModelId;
            _suppressProviderSelectionChangeLoad = false;

            settings.SourceLanguage = defaultSourceLanguage;
            settings.TargetLanguage = defaultTargetLanguage;
            settings.ProgramLanguage = defaultProgramLanguage;
            settings.DirectoryPath = defaultDirectoryPath;
            settings.Prompt = defaultPrompt;
            settings.DeepSeekApiKey = defaultDeepSeekApiKey;
            settings.Gpt4FreeApiKey = defaultGpt4FreeApiKey;
            settings.Gpt4FreeTemperature = defaultGpt4FreeTemperature;
            settings.DeepSeekTemperature = defaultDeepSeekTemperature;
            settings.Gpt4FreeBatchSize = defaultGpt4FreeBatchSize;
            settings.DeepSeekBatchSize = defaultDeepSeekBatchSize;
            settings.TranslationBatchSize = defaultLegacyBatchSize;
            settings.Gpt4FreeProviderId = defaultProviderId;
            settings.Gpt4FreeModelId = defaultModelId;
            settings.Save();

            Providers.Clear();
            Models.Clear();
            OnPropertyChanged(nameof(Providers));
            OnPropertyChanged(nameof(Models));

            ShowModelStatusItem(Properties.Resources.LoadingWindowTitle);
            await LoadProvidersAsync();

            string providerToUse = Providers.FirstOrDefault(p => string.Equals(p.Id, defaultProviderId, StringComparison.Ordinal))?.Id
                ?? SelectedProviderId;

            if (!string.IsNullOrWhiteSpace(providerToUse))
            {
                _suppressProviderSelectionChangeLoad = true;
                SelectedProviderId = providerToUse;
                _suppressProviderSelectionChangeLoad = false;
                await LoadModelsAsync(providerToUse);
            }

            if (!_hasModelStatusItem && Models.Contains(defaultModelId))
            {
                SelectedModelId = defaultModelId;
            }
            else if (!_hasModelStatusItem && Models.Count > 0)
            {
                SelectedModelId = Models[0];
            }

            settings.Gpt4FreeProviderId = SelectedProviderId;
            settings.Gpt4FreeModelId = SelectedModelId;
            settings.Save();

            _dialogService.ShowSuccess(Properties.Resources.SettingsResetToDefaultsMessage);
        }

        private static T GetDefaultSettingValue<T>(string settingName, T fallback)
        {
            var property = Properties.Settings.Default.Properties[settingName];
            object? defaultValue = property?.DefaultValue;
            if (defaultValue is T typedValue)
            {
                return typedValue;
            }

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (defaultValue is string stringValue)
            {
                try
                {
                    if (targetType == typeof(string))
                    {
                        return (T)(object)stringValue;
                    }

                    object convertedFromString = Convert.ChangeType(stringValue, targetType, CultureInfo.InvariantCulture);
                    if (convertedFromString is T convertedTypedValue)
                    {
                        return convertedTypedValue;
                    }
                }
                catch
                {
                }
            }

            if (defaultValue is not null)
            {
                try
                {
                    object converted = Convert.ChangeType(defaultValue, targetType, CultureInfo.InvariantCulture);
                    if (converted is T convertedTypedValue)
                    {
                        return convertedTypedValue;
                    }
                }
                catch
                {
                }
            }

            return fallback;
        }
    }
}
