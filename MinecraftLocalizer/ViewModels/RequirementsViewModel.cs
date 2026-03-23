using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Models.Services.Core;
using System.Windows;
using System.Windows.Input;

namespace MinecraftLocalizer.ViewModels;

public class RequirementsViewModel : ViewModelBase
{
    private readonly IRequirementsService _service;
    private readonly IDialogService _dialogService;

    public DependencyInfoViewModel Python { get; }
    public DependencyInfoViewModel Git { get; }

    public bool CanContinue => Python.Installed && Git.Installed;

    public ICommand ContinueCommand { get; }
    public ICommand CancelCommand { get; }

    public RequirementsViewModel(IRequirementsService service, IDialogService? dialogService = null)
    {
        _service = service;
        _dialogService = dialogService ?? new DialogServiceAdapter();

        // Create DependencyInfo and pass the parent reference for InstallCommand.
        Python = new DependencyInfoViewModel(this, "Python", CheckPythonAsync, service.OpenPythonPage, service.InstallPythonAsync);
        Git = new DependencyInfoViewModel(this, "Git", CheckGitAsync, service.OpenGitPage, service.InstallGitAsync);

        ContinueCommand = new RelayCommand(Continue);
        CancelCommand = new RelayCommand(Cancel);

        // Initial check.
        _ = Python.CheckAsync();
        _ = Git.CheckAsync();
    }

    internal async Task DownloadInstallAsync(
        DependencyInfoViewModel dep,
        Func<IProgress<DownloadProgress>, Task<bool>> installerFunc)
    {
        dep.IsDownloading = true;
        dep.Progress.Reset();

        var progress = new Progress<DownloadProgress>(p => dep.Progress = p);

        bool result = await installerFunc(progress);

        dep.Installed = result && await dep.CheckAsync();
        dep.IsDownloading = false;
        OnPropertyChanged(nameof(CanContinue));
    }

    private async Task<bool> CheckPythonAsync() => await _service.CheckPythonAsync();
    private async Task<bool> CheckGitAsync() => await _service.CheckGitAsync();

    private void Continue()
    {
        if (CanContinue) CloseWindow(true);
        else _dialogService.ShowInformation("Please install all dependencies first.");
    }

    private void Cancel() => CloseWindow(false);

    private void CloseWindow(bool result)
    {
        foreach (Window w in Application.Current.Windows)
            if (w.DataContext == this)
            {
                w.DialogResult = result;
                w.Close();
                break;
            }
    }
}






