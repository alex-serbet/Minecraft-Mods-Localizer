using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Translation;
using System.IO;
using System.Windows;

namespace MinecraftLocalizer.Models.Localization
{
    public partial class TranslationOrchestrator
    {
        private static bool IsValidTranslation(string original, string translated)
        {
            static int CountBracketDifference(ReadOnlySpan<char> text)
            {
                int diff = 0;
                foreach (char c in text)
                {
                    if (c == '[')
                        diff++;
                    if (c == ']')
                        diff--;
                }

                return diff;
            }

            return CountBracketDifference(original) == CountBracketDifference(translated);
        }

        private static Task UpdateUIAsync(Action action)
        {
            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        private static bool AreSameFilePath(string left, string right)
        {
            static string Normalize(string path) =>
                path.Replace('\\', '/').Trim().Trim('/');

            return Normalize(left).Equals(Normalize(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMatchingLanguageFile(string fileName, string sourceLanguage)
        {
            return !string.IsNullOrEmpty(fileName) &&
                   (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".lang", StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsEnUsLocaleFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string fileNameLower = fileName.ToLowerInvariant();
            return fileNameLower.Contains("en_us") && IsLocalizationFile(fileName);
        }

        private static bool IsLocalizationFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return SupportedExtensions.ContainsKey(ext);
        }

        private static void ExpandTranslationBranch(TreeNodeItem rootNode, string sourceFilePath)
        {
            var targetNode = FindNodeByFilePath(rootNode, sourceFilePath);
            if (targetNode == null)
                return;

            var current = targetNode;
            while (current != null)
            {
                current.IsExpanded = true;
                current = current.Parent;
            }
        }

        private static TreeNodeItem? FindNodeByFilePath(TreeNodeItem node, string targetPath)
        {
            if (!string.IsNullOrWhiteSpace(node.FilePath) && AreSameFilePath(node.FilePath, targetPath))
            {
                return node;
            }

            foreach (var child in node.ChildrenNodes)
            {
                var found = FindNodeByFilePath(child, targetPath);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static IEnumerable<TreeNodeItem> GetAllLocalizationFiles(TreeNodeItem node)
        {
            foreach (var child in node.ChildrenNodes)
            {
                if (IsLocalizationFile(child.FileName))
                    yield return child;

                foreach (var subChild in GetAllLocalizationFiles(child))
                    yield return subChild;
            }
        }

        private static ILoadSource CreateLoadSource(TreeNodeItem sourceNode, TreeNodeItem targetNode)
        {
            string sourcePath = IsArchivePath(sourceNode.ModPath) ? sourceNode.ModPath : sourceNode.FilePath;
            return Path.GetExtension(sourcePath) switch
            {
                ".zip" => new ZipLoadSource(sourcePath, targetNode.FilePath),
                ".jar" => new JarLoadSource(sourcePath, targetNode.FilePath),
                _ => new FileLoadSource(targetNode.FilePath)
            };
        }

        private static bool IsArchivePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string ext = Path.GetExtension(path);
            return ext.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".jar", StringComparison.OrdinalIgnoreCase);
        }
    }
}


