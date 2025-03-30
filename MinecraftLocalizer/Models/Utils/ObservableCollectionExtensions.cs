using System.Collections.ObjectModel;

namespace MinecraftLocalizer.Models.Utils
{
    public static class ObservableCollectionExtensions
    {
        public static List<TreeNodeItem> GetCheckedNodes(this ObservableCollection<TreeNodeItem> nodes)
        {
            var checkedNodes = new List<TreeNodeItem>();
            foreach (var node in nodes)
            {
                if (node.IsChecked)
                    checkedNodes.Add(node);

                if (node.ChildrenNodes != null)
                {
                    foreach (var child in node.ChildrenNodes)
                    {
                        if (child.IsChecked)
                            checkedNodes.Add(child);
                    }
                }
            }

            return checkedNodes;
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
