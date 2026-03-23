using MinecraftLocalizer.Interfaces.Ai;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Models.Services.Core;
using MinecraftLocalizer.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Services.Ai
{
    public sealed class Gpt4FreeService : IGpt4FreeService, IDisposable
    {
        private const int ApiPort = 1337;
        private static readonly Regex AnsiRegex = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

        private readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _gpt4FreePath;
        private readonly string _scriptPath;
        private readonly IDialogService _dialogService;

        private CancellationTokenSource _cts = new();
        private readonly List<Process> _runningProcesses = [];
        private readonly LogFeed _consoleOutput = new();
        private bool _disposed;
        private bool _isServerRunning;
        private bool _suppress429Trace;
        private DateTime _last429SeenUtc = DateTime.MinValue;
        private DateTime _suppressTraceUntilUtc = DateTime.MinValue;
        private DateTime _lastProviderErrorLogUtc = DateTime.MinValue;

        public Gpt4FreeService(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new DialogServiceAdapter();
            _gpt4FreePath = Path.Combine(_rootPath, "gpt4free");
            _scriptPath = Path.Combine(_gpt4FreePath, "g4f", "api", "run.py");
        }

        public ILogFeed LogFeed => _consoleOutput;

        /// <summary>
        /// Installs GPT4Free (only once).
        /// </summary>
        public async Task<bool> PerformInstallationAsync()
        {
            ThrowIfDisposed();

            if (Directory.Exists(_gpt4FreePath) && File.Exists(_scriptPath))
            {
                return true;
            }

            _consoleOutput.Reset();
            _consoleOutput.IsRunning = true;
            _cts = new CancellationTokenSource();

            try
            {
                var viewModel = new LogViewModel(this);
                _dialogService.ShowConsoleOutputDialog(viewModel);

                // Check Python
                if (!await CheckPythonInstalledAsync())
                {
                    _consoleOutput.AppendLine("Python 3.10+ not found. Please install Python and add it to PATH.");
                    return false;
                }

                // Clone repository
                await RunProcessAsync(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "clone --progress --verbose https://github.com/xtekky/gpt4free.git",
                    WorkingDirectory = _rootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                });

                // Create virtual environment
                var venvStartInfo = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = "-3 -m venv venv",
                    WorkingDirectory = _gpt4FreePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                ConfigurePythonEncoding(venvStartInfo);
                await RunProcessAsync(venvStartInfo);

                // Install dependencies
                await RunPythonCommandAsync("-m pip install --upgrade pip", "Upgrading pip...");
                await RunPythonCommandAsync("-m pip install -r requirements.txt", "Installing dependencies...");

                _consoleOutput.AppendLine("GPT4Free installation completed successfully.");
                return true;
            }
            catch (OperationCanceledException)
            {
                _consoleOutput.AppendLine("Installation was canceled by the user.");

                KillGpt4FreeProcesses();
                KillGitProcesses();

                await CleanupAfterCancellationAsync();
                return false;
            }
            catch (Exception ex)
            {
                _consoleOutput.AppendLine($"Installation error: {ex.Message}");

                KillGpt4FreeProcesses();
                KillGitProcesses();

                await CleanupAfterCancellationAsync();
                return false;
            }
            finally
            {
                _consoleOutput.IsRunning = false;
            }
        }

        public void EnsureServerRunning()
        {
            if (_isServerRunning)
                return;

            _isServerRunning = true;

            Task.Run(async () =>
            {
                try
                {
                    await StartGpt4FreeAsync();
                }
                catch
                {
                    _isServerRunning = false;
                }
            });
        }

        /// <summary>
        /// Starts GPT4Free API server.
        /// </summary>
        public async Task StartGpt4FreeAsync()
        {
            ThrowIfDisposed();

            _consoleOutput.IsRunning = true;

            try
            {
                if (!Directory.Exists(_gpt4FreePath) || !File.Exists(_scriptPath))
                {
                    _consoleOutput.AppendLine("GPT4Free is not installed. Please run installation first.");
                    return;
                }

                if (IsPortInUse(ApiPort))
                {
                    _consoleOutput.AppendLine($"Port {ApiPort} is already in use. GPT4Free server is probably already running.");
                    return;
                }

                _consoleOutput.AppendLine("Starting GPT4Free API server...");

                var serverStartInfo = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = "-3 -m g4f.api.run",
                    WorkingDirectory = _gpt4FreePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                ConfigurePythonEncoding(serverStartInfo);
                await RunProcessAsync(serverStartInfo, captureOutput: false);

                _consoleOutput.AppendLine("GPT4Free API server started.");
            }
            finally
            {
                _consoleOutput.IsRunning = false;
            }
        }

        /// <summary>
        /// Cancels installation (only if it is currently running).
        /// </summary>
        public async Task CancelInstallationAsync()
        {
            _cts.Cancel();

            KillGpt4FreeProcesses();
            KillGitProcesses();

            await CleanupAfterCancellationAsync();
            _consoleOutput.IsRunning = false;
        }

        private async Task CleanupAfterCancellationAsync()
        {
            if (!Directory.Exists(_gpt4FreePath))
                return;

            RemoveReadOnlyAttributes(_gpt4FreePath);

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Directory.Delete(_gpt4FreePath, true);
                    _consoleOutput.AppendLine("Installation folder removed.");
                    return;
                }
                catch
                {
                    await Task.Delay(500);
                }
            }

            _consoleOutput.AppendLine("Failed to remove installation folder.");
        }

        private static void RemoveReadOnlyAttributes(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);

            foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(dir, FileAttributes.Normal);
        }

        private static void KillGpt4FreeProcesses()
        {
            foreach (var proc in Process.GetProcessesByName("python"))
            {
                try
                {
                    string? cmd = GetProcessCommandLine(proc);
                    if (cmd?.Contains("g4f.api.run") == true)
                    {
                        proc.Kill();
                        proc.WaitForExit();
                    }
                }
                catch { }
            }
        }

        private static void KillGitProcesses()
        {
            foreach (var proc in Process.GetProcessesByName("git"))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit();
                }
                catch { }
            }
        }

        private static string? GetProcessCommandLine(Process process)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

                return searcher.Get()
                    .Cast<ManagementObject>()
                    .FirstOrDefault()?["CommandLine"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task RunProcessAsync(ProcessStartInfo psi, bool captureOutput = true)
        {
            _cts.Token.ThrowIfCancellationRequested();

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _runningProcesses.Add(proc);

            proc.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (captureOutput)
                        AppendFilteredConsoleLine(e.Data);
                }
            };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (captureOutput)
                        AppendFilteredConsoleLine(e.Data);
                }
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using (_cts.Token.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                        proc.Kill(true);
                }
                catch { }
            }))
            {
                await proc.WaitForExitAsync();
            }

            _runningProcesses.Remove(proc);

            _cts.Token.ThrowIfCancellationRequested();

            if (proc.ExitCode != 0)
                throw new Exception($"Process exited with code {proc.ExitCode}: {psi.FileName} {psi.Arguments}");
        }

        private async Task RunPythonCommandAsync(string args, string statusMessage)
        {
            _consoleOutput.AppendLine(statusMessage);

            var commandStartInfo = new ProcessStartInfo
            {
                FileName = "py",
                Arguments = $"-3 {args}",
                WorkingDirectory = _gpt4FreePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            ConfigurePythonEncoding(commandStartInfo);
            await RunProcessAsync(commandStartInfo);
        }

        private static async Task<bool> CheckPythonInstalledAsync()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = "-3 -c \"import sys; print(sys.version_info[:2])\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                ConfigurePythonEncoding(psi);

                using var process = new Process { StartInfo = psi };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var match = System.Text.RegularExpressions.Regex.Match(output, @"\((\d+),\s*(\d+)\)");
                if (!match.Success)
                    return false;

                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);

                return major > 3 || (major == 3 && minor >= 10);
            }
            catch
            {
                return false;
            }
        }

        private static void ConfigurePythonEncoding(ProcessStartInfo startInfo)
        {
            startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            startInfo.Environment["PYTHONUTF8"] = "1";
        }

        private static string NormalizeOutputLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            // Remove ANSI color/control sequences (e.g. "\x1b[32mINFO\x1b[0m").
            var cleaned = AnsiRegex.Replace(line, string.Empty);
            return cleaned.Replace("\0", string.Empty).TrimEnd();
        }

        private void AppendFilteredConsoleLine(string line)
        {
            string cleaned = NormalizeOutputLine(line);
            if (string.IsNullOrWhiteSpace(cleaned))
                return;

            var lower = cleaned.ToLowerInvariant();

            bool is429 =
                lower.Contains("error 429") ||
                lower.Contains("request limit") ||
                lower.Contains("too many requests") ||
                lower.Contains("429: provider returned error");

            bool isProviderErrorLine = lower.StartsWith("error:g4f.api:");

            var trimmed = cleaned.TrimStart();
            bool looksTrace =
                lower.StartsWith("traceback") ||
                trimmed.StartsWith("file \"", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("^") ||
                lower.Contains("g4f.errors.responseerror") ||
                lower.Contains("openaitemplate.py") ||
                lower.Contains("raise_error") ||
                lower.Contains("async for") ||
                lower.Contains("await asyncio.wait_for") ||
                lower.Contains("create_async_generator") ||
                lower.Contains("read_response");

            if (is429)
            {
                _suppress429Trace = true;
                _last429SeenUtc = DateTime.UtcNow;
                _suppressTraceUntilUtc = _last429SeenUtc.AddSeconds(30);
                return;
            }

            if (isProviderErrorLine)
            {
                var now = DateTime.UtcNow;
                if (now - _lastProviderErrorLogUtc >= TimeSpan.FromSeconds(2))
                {
                    _lastProviderErrorLogUtc = now;
                    var message = cleaned["ERROR:g4f.api:".Length..].Trim();
                    _consoleOutput.AppendLine($"GPT4Free provider error: {message}");
                }
                _suppressTraceUntilUtc = now.AddSeconds(10);
                return;
            }

            if (IsGpt4FreeRuntimeLine(lower))
            {
                return;
            }

            if (looksTrace && DateTime.UtcNow - _last429SeenUtc <= TimeSpan.FromSeconds(30))
            {
                return;
            }

            if (looksTrace && DateTime.UtcNow < _suppressTraceUntilUtc)
            {
                return;
            }

            if (_suppress429Trace)
            {
                if (DateTime.UtcNow - _last429SeenUtc > TimeSpan.FromSeconds(5))
                {
                    _suppress429Trace = false;
                }

                if (looksTrace || lower.StartsWith("error:") || lower.StartsWith("exception") || lower.Contains("responseerror"))
                {
                    return;
                }

                _suppress429Trace = false;
            }

            _consoleOutput.AppendLine(cleaned);
        }

        private static bool IsGpt4FreeRuntimeLine(string lowerLine)
        {
            if (string.IsNullOrWhiteSpace(lowerLine))
                return false;

            if (lowerLine.StartsWith("info:") ||
                lowerLine.StartsWith("warning:") ||
                lowerLine.StartsWith("debug:") ||
                lowerLine.StartsWith("error:"))
            {
                return true;
            }

            return lowerLine.Contains("uvicorn") ||
                   lowerLine.Contains("g4f") ||
                   lowerLine.Contains("application startup") ||
                   lowerLine.Contains("starting server") ||
                   lowerLine.Contains("read cookies") ||
                   lowerLine.Contains("http/1.1");
        }

        private static bool IsPortInUse(int port)
        {
            var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return listeners.Any(endpoint => endpoint.Port == port);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Shutdown()
        {
            if (_disposed)
                return;

            _cts.Cancel();
            KillGpt4FreeProcesses();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cts.Cancel();
            KillGpt4FreeProcesses();
            KillGitProcesses();

            _cts.Dispose();

            foreach (var proc in _runningProcesses)
            {
                try
                {
                    if (!proc.HasExited)
                        proc.Kill(true);
                }
                catch { }
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

