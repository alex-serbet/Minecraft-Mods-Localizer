using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.ViewModels;

namespace MinecraftLocalizer.Models.Services.Core
{
    public sealed partial class RequirementsService : IRequirementsService
    {
        private readonly IDialogService _dialogService;

        public RequirementsService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
        }

        public async Task<bool> EnsureRequirementsAsync()
        {
            bool pythonInstalled = await CheckPythonAsync();
            bool gitInstalled = await CheckGitAsync();

            if (pythonInstalled && gitInstalled)
                return true;

            var viewModel = new RequirementsViewModel(this, _dialogService);
            viewModel.Python.Installed = pythonInstalled;
            viewModel.Git.Installed = gitInstalled;

            return _dialogService.ShowRequirementsDialog(viewModel) == true;
        }

        public Task<bool> InstallPythonAsync(IProgress<DownloadProgress> progress) =>
            InstallDependencyAsync(PythonInstaller, progress);

        public Task<bool> InstallGitAsync(IProgress<DownloadProgress> progress) =>
            InstallDependencyAsync(GitInstaller, progress);
    }
}
