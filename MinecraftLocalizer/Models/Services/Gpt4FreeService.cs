using MinecraftLocalizer.Views;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;
using System.ComponentModel;

namespace MinecraftLocalizer.Models.Services
{
    public sealed class Gpt4FreeService : IDisposable
    {
        private const int MaxDeleteAttempts = 3;
        private const int DeleteRetryDelayMs = 500;

        private readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _gpt4FreePath;
        private readonly string _scriptPath;

        private readonly CancellationTokenSource _cts = new();
        private Process? _activeProcess;
        private LoadingWindow? _loadingWindow;
        private bool _disposed;

        public Gpt4FreeService()
        {
            _gpt4FreePath = Path.Combine(_rootPath, "gpt4free");
            _scriptPath = Path.Combine(_gpt4FreePath, "g4f", "api", "run.py");
        }

        public async Task<bool> IsGpt4FreeExistAsync()
        {
            ThrowIfDisposed();

            if (!Directory.Exists(_gpt4FreePath))
            {
                if (!await HandleMissingInstallationAsync())
                    return false;
            }

            if (!File.Exists(_scriptPath))
            {
                DialogService.ShowError("File run.py not found. Installation was unsuccessful.");
                return false;
            }

            return await CheckAndStartServiceAsync();
        }

        private async Task<bool> HandleMissingInstallationAsync()
        {
            if (!DialogService.ShowConfirmation("GPT4Free is not installed. Install now?","Installing GPT4Free"))
                return false;

            return await PerformInstallationAsync();
        }

        private async Task<bool> CheckAndStartServiceAsync()
        {
            if (await IsGpt4FreeRunningAsync())
                return true;

            if (!DialogService.ShowConfirmation("GPT4Free is not running. Start now?", "Starting GPT4Free"))
                return false;

            await StartGpt4FreeAsync();
            return true;
        }

        private async Task<bool> PerformInstallationAsync()
        {
            try
            {
                using (_loadingWindow = new LoadingWindow(Application.Current.MainWindow))
                {
                    _loadingWindow.CancelRequested += OnInstallationCanceled;
                    _loadingWindow.Show();

                    await ExecuteProcessAsync("install_gpt4free.bat");

                    return ValidateInstallationResult();
                }
            }
            finally
            {
                _loadingWindow?.Close();
            }
        }

        private async Task ExecuteProcessAsync(string batchFile)
        {
            var psi = CreateProcessStartInfo(batchFile);
            _activeProcess = new Process { StartInfo = psi };

            AttachOutputHandlers();
            _activeProcess.Start();
            _activeProcess.BeginOutputReadLine();
            _activeProcess.BeginErrorReadLine();

            await _activeProcess.WaitForExitAsync(_cts.Token);
        }

        private ProcessStartInfo CreateProcessStartInfo(string batchFile) => new()
        {
            FileName = "cmd.exe",
            Arguments = $"/c {batchFile}",
            WorkingDirectory = _rootPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        private void AttachOutputHandlers()
        {
            var handler = new DataReceivedEventHandler((s, e) =>
                HandleProcessOutput(e.Data));

            _activeProcess!.OutputDataReceived += handler;
            _activeProcess.ErrorDataReceived += handler;
        }

        private async void HandleProcessOutput(string? data)
        {
            if (string.IsNullOrEmpty(data)) return;

            Debug.WriteLine(data);
            if (_loadingWindow != null)
                await _loadingWindow.UpdateProgressGpt4FreeAsync(data, _cts.Token);
        }

        private bool ValidateInstallationResult()
        {
            if (_cts.IsCancellationRequested) return false;

            if (_activeProcess?.ExitCode != 0)
            {
                DialogService.ShowError("GPT4Free installation error");
                return false;
            }

            return Directory.Exists(_gpt4FreePath) && File.Exists(_scriptPath);
        }

        private async void OnInstallationCanceled(object? sender, EventArgs e)
        {
            try
            {
                _cts.Cancel();
                await TerminateActiveProcessAsync();
                await CleanupAfterCancellationAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cancellation: {ex.Message}");
            }
        }

        private async Task TerminateActiveProcessAsync()
        {
            if (_activeProcess is null || _activeProcess.HasExited)
                return;

            try
            {
                _activeProcess.Kill();
                await _activeProcess.WaitForExitAsync();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
            {
                Debug.WriteLine("Access denied when terminating the process");
            }
        }

        private async Task CleanupAfterCancellationAsync()
        {
            for (int i = 1; i <= MaxDeleteAttempts; i++)
            {
                try
                {
                    if (Directory.Exists(_gpt4FreePath))
                    {
                        Directory.Delete(_gpt4FreePath, recursive: true);
                        Debug.WriteLine("Installation folder deleted");
                        return;
                    }
                }
                catch (IOException ex) when (i < MaxDeleteAttempts)
                {
                    Debug.WriteLine($"Attempt {i}: {ex.Message}");
                    await Task.Delay(DeleteRetryDelayMs);
                }
            }
        }

        public static async Task<bool> IsGpt4FreeRunningAsync()
        {
            return await Task.Run(() =>
            {
                foreach (var process in Process.GetProcessesByName("python"))
                {
                    try
                    {
                        if (GetProcessCommandLine(process)?.Contains("g4f.api.run") == true)
                            return true;
                    }
                    catch {  }
                }
                return false;
            });
        }

        private static string? GetProcessCommandLine(Process process)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

                return searcher.Get()
                    .Cast<ManagementObject>()
                    .FirstOrDefault()?["CommandLine"]?
                    .ToString();
            }
            catch
            {
                return null;
            }
        }

        public Task StartGpt4FreeAsync()
        {
            var psi = CreateProcessStartInfo("run_gpt4free.bat");
            _activeProcess = new Process { StartInfo = psi };

            AttachOutputHandlers();
            _activeProcess.Start();
            _activeProcess.BeginOutputReadLine();
            _activeProcess.BeginErrorReadLine();

            return Task.CompletedTask;
        }

        public static void KillGpt4FreeProcess()
        {
            foreach (var process in Process.GetProcessesByName("python"))
            {
                try
                {
                    string? commandLine = GetProcessCommandLine(process);
                    if (commandLine?.Contains("g4f.api.run") != true)
                        continue;

                    process.Kill();
                    process.WaitForExit();

                    Debug.WriteLine("GPT4Free process successfully terminated.");

                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error when terminating the process: {ex.Message}");
                }
            }

            Debug.WriteLine("GPT4Free process not found.");
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cts.Cancel();
            _cts.Dispose();
            _activeProcess?.Dispose();
            _loadingWindow?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}