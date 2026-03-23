using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Services.Core
{
    public sealed partial class RequirementsService
    {
        public void OpenPythonPage()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.python.org/downloads/",
                UseShellExecute = true
            });
        }

        public void OpenGitPage()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://git-scm.com/downloads",
                UseShellExecute = true
            });
        }

        public async Task<bool> CheckPythonAsync()
        {
            try
            {
                string output = await RunAndReadAsync("py", "-3 -c \"import sys; print(sys.version_info[:2])\"");
                var match = Regex.Match(output, @"\((\d+),\s*(\d+)\)");

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

        public async Task<bool> CheckGitAsync()
        {
            try
            {
                await RunAndReadAsync("git", "--version");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CheckPortablePython()
        {
            try
            {
                string portablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "portable");

                return Directory.Exists(portablePath) &&
                       Directory.GetFiles(portablePath, "python.exe", SearchOption.AllDirectories).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<string> RunAndReadAsync(string fileName, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output;
        }
    }
}
