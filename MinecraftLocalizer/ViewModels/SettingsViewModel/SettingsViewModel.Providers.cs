using System.Diagnostics;
using System.Text.Json;

namespace MinecraftLocalizer.ViewModels
{
    public partial class SettingsViewModel
    {
        private void OnSelectedProviderChanged()
        {
            if (_suppressProviderSelectionChangeLoad)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedProviderId))
            {
                Models.Clear();
                SetHasModelStatusItem(false);
                SelectedModelId = string.Empty;
                OnPropertyChanged(nameof(Models));
                OnPropertyChanged(nameof(IsModelSelectionEnabled));
                return;
            }

            _ = LoadModelsAsync(SelectedProviderId);
        }

        private async Task LoadProvidersAsync()
        {
            IsProvidersLoading = true;
            ProviderLoadError = string.Empty;

            try
            {
                using var response = await _httpClient.GetAsync(Gpt4FreeProvidersEndpoint);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);

                var providers = ParseProviders(document.RootElement);

                Providers.Clear();
                foreach (var provider in providers.OrderBy(p => p.Label, StringComparer.OrdinalIgnoreCase))
                {
                    Providers.Add(provider);
                }
                _hasProviderStatusItem = false;
                OnPropertyChanged(nameof(Providers));
                OnPropertyChanged(nameof(IsProviderSelectionEnabled));

                if (Providers.Count == 0)
                {
                    ProviderLoadError = Properties.Resources.NoProvidersReturnedMessage;
                    Providers.Add(new ProviderOption { Id = string.Empty, Label = Properties.Resources.DialogServiceErrorTitle });
                    _hasProviderStatusItem = true;
                    SelectedProviderId = string.Empty;
                    OnPropertyChanged(nameof(Providers));
                    OnPropertyChanged(nameof(IsProviderSelectionEnabled));

                    Models.Clear();
                    Models.Add(Properties.Resources.DialogServiceErrorTitle);
                    SetHasModelStatusItem(true);
                    SelectedModelId = Properties.Resources.DialogServiceErrorTitle;
                    OnPropertyChanged(nameof(Models));
                    return;
                }

                string savedProvider = Properties.Settings.Default.Gpt4FreeProviderId;
                var selected = Providers.FirstOrDefault(p => string.Equals(p.Id, savedProvider, StringComparison.OrdinalIgnoreCase))
                    ?? Providers.FirstOrDefault();
                if (selected != null)
                {
                    _suppressProviderSelectionChangeLoad = true;
                    SelectedProviderId = selected.Id;
                    _suppressProviderSelectionChangeLoad = false;
                    _ = LoadModelsAsync(selected.Id);
                }
            }
            catch (Exception ex)
            {
                ProviderLoadError = $"{Properties.Resources.FailedToLoadProvidersMessage}: {ex.Message}";
                Providers.Clear();
                Providers.Add(new ProviderOption { Id = string.Empty, Label = Properties.Resources.DialogServiceErrorTitle });
                SelectedProviderId = string.Empty;
                _hasProviderStatusItem = true;
                OnPropertyChanged(nameof(Providers));
                OnPropertyChanged(nameof(IsProviderSelectionEnabled));

                Models.Clear();
                Models.Add(Properties.Resources.DialogServiceErrorTitle);
                SetHasModelStatusItem(true);
                SelectedModelId = Properties.Resources.DialogServiceErrorTitle;
                OnPropertyChanged(nameof(Models));
            }
            finally
            {
                IsProvidersLoading = false;
            }
        }

        private async Task LoadModelsAsync(string providerId)
        {
            int requestVersion = Interlocked.Increment(ref _modelRequestVersion);
            IsModelsLoading = true;
            ModelLoadError = string.Empty;
            ShowModelStatusItem(Properties.Resources.LoadingWindowTitle);

            try
            {
                using var response = await _httpClient.GetAsync($"{Gpt4FreeProvidersEndpoint}/{Uri.EscapeDataString(providerId)}");
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);

                if (requestVersion != _modelRequestVersion)
                {
                    return;
                }

                var modelIds = ParseModelIds(document.RootElement);

                Models.Clear();
                SetHasModelStatusItem(false);
                foreach (var modelId in modelIds)
                {
                    Models.Add(modelId);
                }
                OnPropertyChanged(nameof(Models));

                if (Models.Count == 0)
                {
                    ModelLoadError = Properties.Resources.NoModelsReturnedMessage;
                    ShowModelStatusItem(ModelStatusText);
                    return;
                }

                string savedModel = Properties.Settings.Default.Gpt4FreeModelId;
                if (Models.Contains(savedModel))
                {
                    SelectedModelId = savedModel;
                }
                else
                {
                    SelectedModelId = Models[0];
                }
            }
            catch (Exception ex)
            {
                if (requestVersion != _modelRequestVersion)
                {
                    return;
                }

                Models.Clear();
                Models.Add(Properties.Resources.DialogServiceErrorTitle);
                SetHasModelStatusItem(true);
                SelectedModelId = Properties.Resources.DialogServiceErrorTitle;
                OnPropertyChanged(nameof(Models));
                ModelLoadError = $"{Properties.Resources.FailedToLoadModelsMessage}: {ex.Message}";
                OnPropertyChanged(nameof(ModelStatusText));
            }
            finally
            {
                if (requestVersion == _modelRequestVersion)
                {
                    IsModelsLoading = false;
                    OnPropertyChanged(nameof(IsModelSelectionEnabled));
                    OnPropertyChanged(nameof(ModelStatusText));
                }
            }
        }

        private void ShowModelStatusItem(string statusText)
        {
            string text = string.IsNullOrWhiteSpace(statusText) ? " " : statusText;
            Models.Clear();
            Models.Add(text);
            SelectedModelId = text;
            SetHasModelStatusItem(true);
            OnPropertyChanged(nameof(Models));
            OnPropertyChanged(nameof(IsModelSelectionEnabled));
        }

        private void SetHasModelStatusItem(bool value)
        {
            if (_hasModelStatusItem == value)
            {
                return;
            }

            _hasModelStatusItem = value;
            OnPropertyChanged(nameof(IsModelSelectionEnabled));
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private static List<ProviderOption> ParseProviders(JsonElement root)
        {
            JsonElement providersNode = root;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("data", out JsonElement dataNode))
            {
                providersNode = dataNode;
            }

            var result = new List<ProviderOption>();
            if (providersNode.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var item in providersNode.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string? id = TryGetString(item, "id");
                string? label = TryGetString(item, "label");

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                result.Add(new ProviderOption
                {
                    Id = id,
                    Label = label
                });
            }

            return result;
        }

        private static List<string> ParseModelIds(JsonElement root)
        {
            JsonElement source = root;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("data", out JsonElement dataNode))
            {
                source = dataNode;
            }

            if (source.ValueKind != JsonValueKind.Object ||
                !source.TryGetProperty("models", out JsonElement modelsNode))
            {
                return [];
            }

            var modelIds = new HashSet<string>(StringComparer.Ordinal);

            if (modelsNode.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in modelsNode.EnumerateObject())
                {
                    if (!string.IsNullOrWhiteSpace(property.Name))
                    {
                        modelIds.Add(property.Name);
                    }
                }
            }
            else if (modelsNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in modelsNode.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        string modelId = item.GetString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(modelId))
                        {
                            modelIds.Add(modelId);
                        }

                        continue;
                    }

                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    string? modelIdValue = TryGetString(item, "id");
                    if (!string.IsNullOrWhiteSpace(modelIdValue))
                    {
                        modelIds.Add(modelIdValue);
                    }
                }
            }

            return modelIds.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string? TryGetString(JsonElement node, string propertyName)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }
            }

            return null;
        }
    }
}
