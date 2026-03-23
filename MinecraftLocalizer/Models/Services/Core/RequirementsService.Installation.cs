using MinecraftLocalizer.Properties;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace MinecraftLocalizer.Models.Services.Core
{
    public sealed partial class RequirementsService
    {
        private sealed class DependencyInstaller
        {
            public string Name { get; init; } = string.Empty;
            public string DownloadUrl { get; init; } = string.Empty;
            public string FileName { get; init; } = string.Empty;
            public string InstallArgs { get; init; } = string.Empty;
        }

        private static readonly DependencyInstaller PythonInstaller = new()
        {
            Name = "Python",
            DownloadUrl = "https://www.python.org/ftp/python/3.12.0/python-3.12.0-amd64.exe",
            FileName = "python-installer.exe",
            InstallArgs = "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0"
        };

        private static readonly DependencyInstaller GitInstaller = new()
        {
            Name = "Git",
            DownloadUrl = "https://github.com/git-for-windows/git/releases/download/v2.53.0.windows.1/Git-2.53.0-64-bit.exe",
            FileName = "git-installer.exe",
            InstallArgs = "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS"
        };

        private async Task<bool> InstallDependencyAsync(
            DependencyInstaller installer,
            IProgress<DownloadProgress>? progress)
        {
            try
            {
                string installerPath = Path.Combine(Path.GetTempPath(), installer.FileName);
                var state = new DownloadProgress(installer.Name)
                {
                    IsDownloading = true,
                    Status = Resources.StartingInstallerDownload
                };

                progress?.Report(state);

                await DownloadFileAsync(installer.DownloadUrl, installerPath, state, progress);

                state.Progress = 50;
                state.Status = Resources.DownloadComplete;
                progress?.Report(state);

                int exitCode = await RunProcessAsync(installerPath, installer.InstallArgs);
                await SimulateInstallProgress(state, progress);

                if (exitCode != 0)
                    return ReportInstallError(installer.Name, exitCode, progress);

                state.Status = Resources.InstallationComplete;
                state.IsCompleted = true;
                state.IsDownloading = false;
                progress?.Report(state);

                TryDelete(installerPath);
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report(new DownloadProgress(installer.Name)
                {
                    Status = $"Error: {ex.Message}",
                    HasError = true
                });

                return false;
            }
        }

        private static async Task DownloadFileAsync(
            string url,
            string path,
            DownloadProgress state,
            IProgress<DownloadProgress>? progress)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var file = new FileStream(path, FileMode.Create);

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes <= 0)
                    continue;

                state.Progress = (double)totalRead / totalBytes * 50;
                state.Status = $"{Resources.Downloading}: {FormatBytes(totalRead)} / {FormatBytes(totalBytes)}";
                progress?.Report(state);
            }
        }

        private static async Task<int> RunProcessAsync(string fileName, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode;
        }

        private static async Task SimulateInstallProgress(
            DownloadProgress state,
            IProgress<DownloadProgress>? progress)
        {
            for (int percent = 51; percent <= 100; percent++)
            {
                await Task.Delay(10);
                state.Progress = percent;
                state.Status = $"{Resources.Installing} {percent}%";
                progress?.Report(state);
            }
        }
    }
}
