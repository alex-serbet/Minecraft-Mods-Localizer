using MinecraftLocalizer.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace MinecraftLocalizer.Models.Utils
{
    public static class ObservableCollectionExtensions
    {
        public static List<TreeNodeItem> GetCheckedNodes(this ObservableCollection<TreeNodeItem> nodes)
        {
            var checkedNodes = new List<TreeNodeItem>();
            foreach (var node in nodes)
            {
                CollectCheckedNodes(node, checkedNodes);
            }

            return checkedNodes;
        }

        private static void CollectCheckedNodes(
            TreeNodeItem node,
            List<TreeNodeItem> checkedNodes)
        {
            if (node.IsChecked == true)
            {
                if (HasUncheckedDescendant(node))
                {
                    foreach (var child in node.ChildrenNodes)
                    {
                        CollectCheckedNodes(child, checkedNodes);
                    }
                }
                else
                {
                    checkedNodes.Add(node);
                }

                return;
            }

            foreach (var child in node.ChildrenNodes)
            {
                CollectCheckedNodes(child, checkedNodes);
            }
        }

        private static bool HasUncheckedDescendant(TreeNodeItem node)
        {
            foreach (var child in node.ChildrenNodes)
            {
                if (child.IsChecked != true)
                    return true;

                if (HasUncheckedDescendant(child))
                    return true;
            }

            return false;
        }

        public static List<TreeNodeItem> ExpandToLocalizationFiles(this List<TreeNodeItem> nodes)
        {
            var results = new HashSet<TreeNodeItem>();

            foreach (var node in nodes)
            {
                CollectLocalizationFiles(node, results);
            }

            return results.Count == 0 ? nodes : results.ToList();
        }

        private static void CollectLocalizationFiles(TreeNodeItem node, HashSet<TreeNodeItem> results)
        {
            if (IsLocalizationFile(node.FileName))
            {
                results.Add(node);
                return;
            }

            foreach (var child in node.ChildrenNodes)
            {
                CollectLocalizationFiles(child, results);
            }
        }

        private static bool IsLocalizationFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".lang", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".snbt", StringComparison.OrdinalIgnoreCase);
        }

        public static void RemoveTranslatingState(this ObservableCollection<TreeNodeItem> nodes)
        {
            foreach (var node in nodes)
            {
                node.IsTranslating = false;

                RemoveTranslatingState(node.ChildrenNodes);
            }
        }

        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();

            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}


