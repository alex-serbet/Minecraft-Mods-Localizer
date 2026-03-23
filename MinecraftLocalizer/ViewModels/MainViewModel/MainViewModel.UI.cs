using MinecraftLocalizer.Models;
using MinecraftLocalizer.Models.Services.Core;
using MinecraftLocalizer.Models.Utils;
using System.Diagnostics;
using System.Windows;

namespace MinecraftLocalizer.ViewModels
{
    public partial class MainViewModel
    {
        private void UpdateProgress(int current, int total, double percentage)
        {
            if ((DateTime.Now - _lastProgressUpdate).TotalMilliseconds < 300)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                TranslationProgress = string.Format(
                    Properties.Resources.TranslationStatusRunning,
                    current,
                    total,
                    percentage
                );
            });

            _lastProgressUpdate = DateTime.Now;
        }

        private void UpdateTreeNodesLogoVisibility()
        {
            IsTreeNodesLogoVisible = TreeNodes.Count == 0;
        }

        private void UpdateDataGridLogoVisibility()
        {
            IsDataGridLogoVisible = LocalizationStrings.Count == 0;
        }

        private void UpdateStreamingTextLogoVisibility()
        {
            IsStreamingTextLogoVisible = string.IsNullOrWhiteSpace(StreamingText);
        }

        private void UpdateConsoleOutputLogoVisibility()
        {
            IsConsoleOutputLogoVisible = string.IsNullOrWhiteSpace(ConsoleOutputText);
        }

        private void SelectAllItems(bool isSelected)
        {
            if (DataGridCollectionView != null)
            {
                foreach (LocalizationItem item in DataGridCollectionView)
                {
                    item.IsSelected = isSelected;
                }
            }
        }

        private async void OnApplicationExit()
        {
            try
            {
                await EnsureGpt4FreeServiceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error killing GPT4Free process on exit: {ex.Message}");
            }
        }

        private void ToggleViewMode() =>
            IsRawViewMode = !IsRawViewMode;

        private void ToggleConsoleOutput()
        {
            ShowConsoleOutput = !ShowConsoleOutput;
            UpdateConsoleOutputLogoVisibility();
        }

        private void OnConsoleOutputChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(LogFeed.Output))
                {
                    ConsoleOutputText = _gpt4FreeService.LogFeed.Output;
                    ConsoleOutputScrolled?.Invoke();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void OnRawContentChanged(object? sender, string content)
        {
            if (LocalizationText != content)
            {
                LocalizationText = content;
            }
        }

        private void CollapseConsole()
        {
            StreamingTextRowHeight = IsStreamingButtonCollapsed
                ? new GridLength(40, GridUnitType.Pixel)
                : new GridLength(220, GridUnitType.Pixel);
        }

        private async void RefreshDataOnViewModeChanged()
        {
            var selectedNode = TreeNodes.GetCheckedNodes().FirstOrDefault();
            if (selectedNode != null)
            {
                await OnTreeViewItemSelectedAsync(selectedNode);
            }

            DataGridCollectionView?.Refresh();
        }
    }
}




