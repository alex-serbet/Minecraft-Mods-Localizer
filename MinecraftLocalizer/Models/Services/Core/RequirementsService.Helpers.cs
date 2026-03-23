using System.IO;

namespace MinecraftLocalizer.Models.Services.Core
{
    public sealed partial class RequirementsService
    {
        private static bool ReportInstallError(
            string name,
            int code,
            IProgress<DownloadProgress>? progress)
        {
            progress?.Report(new DownloadProgress(name)
            {
                Status = $"Installation error (code {code})",
                HasError = true
            });

            return false;
        }

        private static void TryDelete(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int suffixIndex = 0;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }
    }
}
