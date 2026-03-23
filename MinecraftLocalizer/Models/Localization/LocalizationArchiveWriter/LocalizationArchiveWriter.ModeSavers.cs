using MinecraftLocalizer.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Localization
{
    public partial class LocalizationArchiveWriter
    {
        public static string? TryGetOneFileExtension(TreeNodeItem node)
        {
            return GetFileExtension(node);
        }

        public static string SaveSingleFileToPath(
            TreeNodeItem node,
            IEnumerable<LocalizationItem> localizationStrings,
            string localizationText,
            string outputPath,
            bool isRawViewMode)
        {
            if (node?.FileName == null)
                throw new InvalidOperationException("File name is not specified for One File mode.");

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException("Output path is not specified.");

            var fileFormat = Path.GetExtension(outputPath).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fileFormat))
            {
                fileFormat = GetFileExtension(node) ?? "json";
                outputPath = Path.ChangeExtension(outputPath, fileFormat);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false));
            if (isRawViewMode)
                WriteRawContent(writer, localizationText);
            else
                WriteLocalizationContent(writer, fileFormat, localizationStrings);

            return outputPath;
        }

        public static void SeedPatchouliArchive(TreeNodeItem node)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.ModPath))
                return;

            string modPath = node.ModPath;
            string ext = Path.GetExtension(modPath);
            if (!ext.Equals(".jar", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return;

            string resourcePacksDir = Path.Combine(Settings.DirectoryPath, "resourcepacks");
            Directory.CreateDirectory(resourcePacksDir);
            string zipPath = Path.Combine(resourcePacksDir, "MinecraftLocalizer.zip");

            using var zipStream = new FileStream(zipPath, FileMode.OpenOrCreate);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Update, true);
            EnsureResourcePackMetadata(archive);

            using var sourceArchive = ZipFile.OpenRead(modPath);
            var regex = LocaleRegex();
            string targetLanguage = Settings.TargetLanguage;

            foreach (var fileNode in EnumerateLocalizationFiles(node))
            {
                if (string.IsNullOrWhiteSpace(fileNode.FilePath))
                    continue;

                string sourcePath = fileNode.FilePath.Replace("\\", "/");
                var sourceEntry = sourceArchive.GetEntry(sourcePath);
                if (sourceEntry == null)
                    continue;

                string? updatedPath = GetUpdatedPatchouliPath(sourcePath, regex, targetLanguage);
                if (string.IsNullOrWhiteSpace(updatedPath))
                    continue;

                archive.GetEntry(updatedPath)?.Delete();
                using var targetStream = archive.CreateEntry(updatedPath).Open();
                using var sourceStream = sourceEntry.Open();
                sourceStream.CopyTo(targetStream);
            }
        }

        private static void SaveNodeLocalization(
          TreeNodeItem node,
          ObservableCollection<LocalizationItem> localizationStrings,
          string localizationText,
          ZipArchive archive,
          TranslationModeType modeType)
        {
            if (node?.FileName == null)
                return;
            if (modeType != TranslationModeType.OneFile && localizationStrings.Count == 0)
                return;

            var targetLanguage = Settings.TargetLanguage;

            switch (modeType)
            {
                case TranslationModeType.Quests:
                    _ = SaveQuestFile(node, localizationStrings);
                    break;

                case TranslationModeType.BetterQuesting:
                    SaveBetterQuestingFile(node, localizationStrings, targetLanguage);
                    break;

                case TranslationModeType.Patchouli:
                    SavePatchouliFiles(node, localizationStrings, archive, targetLanguage);
                    break;

                case TranslationModeType.Mods:
                    SaveModLocalization(node, localizationStrings, archive, targetLanguage);
                    break;

                case TranslationModeType.ResourcePack:
                    SaveModLocalization(node, localizationStrings, archive, targetLanguage);
                    break;

                case TranslationModeType.OneFile:
                    SaveSingleFile(node, localizationStrings, localizationText, archive);
                    break;
            }
        }

        private static string? SaveQuestFile(TreeNodeItem node, IEnumerable<LocalizationItem> strings)
        {
            var fileFormat = GetFileExtension(node) ?? "json";
            var basePath = fileFormat == "json"
                ? Path.Combine(Settings.DirectoryPath, "kubejs", "assets", "kubejs", "lang")
                : Path.Combine(Settings.DirectoryPath, "config", "ftbquests", "quests", "lang");

            Directory.CreateDirectory(basePath);
            var outputFile = Path.Combine(basePath, $"{Settings.TargetLanguage}.{fileFormat}");

            using var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false));
            WriteLocalizationContent(writer, fileFormat, strings);
            return outputFile;
        }

        private static string? GetFileExtension(TreeNodeItem node)
        {
            static string? NormalizeExtension(string? fileName)
            {
                var ext = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant();
                return string.IsNullOrWhiteSpace(ext) ? null : ext;
            }

            var nodeExtension = NormalizeExtension(node.FileName);
            if (!string.IsNullOrWhiteSpace(nodeExtension))
            {
                return nodeExtension;
            }

            var childExtension = NormalizeExtension(node.ChildrenNodes.FirstOrDefault()?.FileName);
            return childExtension;
        }

        private static string? SaveBetterQuestingFile(
            TreeNodeItem node,
            IEnumerable<LocalizationItem> strings,
            string targetLanguage)
        {
            if (node.FileName == null)
                return null;

            var sourceNode = FindLocalizationChild(node) ?? node;
            if (sourceNode.FilePath == null || sourceNode.FileName == null)
                return null;

            var extension = Path.GetExtension(sourceNode.FileName).ToLowerInvariant();
            var directory = Path.GetDirectoryName(sourceNode.FilePath);
            if (string.IsNullOrWhiteSpace(directory))
                return null;

            Directory.CreateDirectory(directory);
            var outputPath = Path.Combine(directory, $"{targetLanguage}{extension}");

            var comments = ExtractComments(sourceNode.FilePath);
            using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false));
            WriteLocalizationContent(writer, extension.TrimStart('.'), strings, comments);
            return outputPath;
        }

        private static TreeNodeItem? FindLocalizationChild(TreeNodeItem node)
        {
            return node.ChildrenNodes.FirstOrDefault(n =>
                n.FileName != null &&
                LocaleRegex().IsMatch(Path.GetFileNameWithoutExtension(n.FileName)));
        }

        private static List<string> ExtractComments(string filePath)
        {
            var comments = new List<string>();

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith('#'))
                    comments.Add(line);
                else if (line.Contains('='))
                    break;
            }

            return comments;
        }

        private static void SavePatchouliFiles(
            TreeNodeItem node,
            IEnumerable<LocalizationItem> strings,
            ZipArchive archive,
            string targetLanguage)
        {
            var stack = new Stack<TreeNodeItem>();
            stack.Push(node);
            var regex = LocaleRegex();

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current.ChildrenNodes.Count == 0 && current.FilePath != null)
                {
                    UpdateLocalizationPath(current.FilePath, regex, targetLanguage, archive, strings);
                }
                else
                {
                    foreach (var child in current.ChildrenNodes)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        private static void UpdateLocalizationPath(
            string filePath,
            Regex regex,
            string targetLanguage,
            ZipArchive archive,
            IEnumerable<LocalizationItem> strings)
        {
            var pathParts = filePath.Replace("\\", "/").Split('/');
            var langIndex = Array.FindIndex(pathParts, p => regex.IsMatch(p));

            if (langIndex < 0)
                return;

            pathParts[langIndex] = targetLanguage;
            var updatedPath = string.Join("/", pathParts);
            var fileFormat = Path.GetExtension(updatedPath).TrimStart('.').ToLowerInvariant();

            archive.GetEntry(updatedPath)?.Delete();
            using var entryStream = archive.CreateEntry(updatedPath).Open();
            using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));

            WriteLocalizationContent(writer, fileFormat, strings);
        }

        private static string? GetUpdatedPatchouliPath(string filePath, Regex regex, string targetLanguage)
        {
            var pathParts = filePath.Replace("\\", "/").Split('/');
            var langIndex = Array.FindIndex(pathParts, p => regex.IsMatch(p));

            if (langIndex < 0)
                return null;

            pathParts[langIndex] = targetLanguage;
            return string.Join("/", pathParts);
        }

        private static IEnumerable<TreeNodeItem> EnumerateLocalizationFiles(TreeNodeItem node)
        {
            foreach (var child in node.ChildrenNodes)
            {
                if (IsLocalizationFileName(child.FileName))
                    yield return child;

                foreach (var subChild in EnumerateLocalizationFiles(child))
                    yield return subChild;
            }
        }

        private static bool IsLocalizationFileName(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".lang", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".snbt", StringComparison.OrdinalIgnoreCase);
        }

        private static void SaveModLocalization(
            TreeNodeItem node,
            IEnumerable<LocalizationItem> strings,
            ZipArchive archive,
            string targetLanguage)
        {
            if (node.FileName == null)
                return;

            string entryPath;
            string fileFormat;

            if (Path.GetExtension(node.FileName) is ".json" or ".lang")
            {
                entryPath = Path.Combine(
                    Path.GetDirectoryName(node.FilePath) ?? "",
                    $"{targetLanguage}{Path.GetExtension(node.FileName)}")
                    .Replace("\\", "/");

                fileFormat = Path.GetExtension(node.FileName).TrimStart('.');
            }
            else
            {
                var childNode = FindLocalizationChild(node);
                if (childNode?.FilePath == null)
                    return;

                var childExtension = Path.GetExtension(childNode.FileName);
                entryPath = Path.Combine(
                    Path.GetDirectoryName(childNode.FilePath) ?? "",
                    $"{targetLanguage}{childExtension}")
                    .Replace("\\", "/");

                fileFormat = childExtension.TrimStart('.');
            }

            SaveToArchive(archive, entryPath, writer =>
                WriteLocalizationContent(writer, fileFormat, strings));
        }

        private static void SaveSingleFile(
            TreeNodeItem node,
            ObservableCollection<LocalizationItem> localizationStrings,
            string localizationText,
            ZipArchive archive)
        {
            if (node?.FileName == null)
                return;

            string entryPath;
            string fileFormat;

            static string GetRelativePath(string fullPath)
            {
                int idx = fullPath.IndexOf("minecraft\\", StringComparison.OrdinalIgnoreCase);
                return idx < 0 ? Path.GetFileName(fullPath) : fullPath[(idx + 10)..];
            }

            if (Path.GetExtension(node.FileName) is ".json" or ".lang" or ".snbt")
            {
                var relativePath = GetRelativePath(node.FilePath);
                entryPath = relativePath.Replace("\\", "/");
                fileFormat = Path.GetExtension(node.FileName).TrimStart('.');
            }
            else
            {
                var childNode = FindLocalizationChild(node);
                if (childNode?.FilePath == null)
                    return;

                var childExtension = Path.GetExtension(childNode.FileName);
                var relativePath = GetRelativePath(childNode.FilePath);
                var dirName = Path.GetDirectoryName(relativePath) ?? "";

                entryPath = Path.Combine(dirName, $"{Settings.TargetLanguage}{childExtension}")
                    .Replace("\\", "/");

                fileFormat = childExtension.TrimStart('.');
            }

            SaveToArchive(archive, entryPath, writer =>
            {
                if (_isRawViewMode)
                    WriteRawContent(writer, localizationText);
                else
                    WriteLocalizationContent(writer, fileFormat, localizationStrings);
            });
        }

        private static void SaveToArchive(ZipArchive archive, string entryPath, Action<TextWriter> writeAction)
        {
            archive.GetEntry(entryPath)?.Delete();
            using var entryStream = archive.CreateEntry(entryPath).Open();
            using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
            writeAction(writer);
        }
    }
}





