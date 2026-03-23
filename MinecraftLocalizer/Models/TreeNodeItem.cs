using MinecraftLocalizer.Properties;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace MinecraftLocalizer.Models
{
    public class TreeNodeItem : INotifyPropertyChanged
    {
        public TreeNodeItem()
        {
            _childrenNodes.CollectionChanged += OnChildrenCollectionChanged;
        }

        private ObservableCollection<TreeNodeItem> _childrenNodes = [];
        public ObservableCollection<TreeNodeItem> ChildrenNodes
        {
            get => _childrenNodes;
            set
            {
                if (ReferenceEquals(_childrenNodes, value))
                    return;

                _childrenNodes.CollectionChanged -= OnChildrenCollectionChanged;
                _childrenNodes = value ?? [];
                _childrenNodes.CollectionChanged += OnChildrenCollectionChanged;

                foreach (var child in _childrenNodes)
                {
                    child.Parent = this;
                }
            }
        }
        public TreeNodeItem? Parent { get; private set; }
        public bool IsRoot { get; set; }
        public bool HasItems => ChildrenNodes.Any();
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public required string ModPath { get; set; }


        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                SetCheckedInternal(value, propagateToChildren: true, updateParents: true);
            }
        }

        private void SetCheckedInternal(bool? value, bool propagateToChildren, bool updateParents)
        {
            if (_isChecked == value)
                return;

            _isChecked = value;
            OnPropertyChanged(nameof(IsChecked));

            if (propagateToChildren && value.HasValue)
            {
                SetChildrenChecked(value.Value);
            }

            if (updateParents)
            {
                Parent?.UpdateCheckFromChildren();
            }
        }

        private void SetChildrenChecked(bool isChecked)
        {
            if (ChildrenNodes == null || ChildrenNodes.Count == 0)
                return;

            if (!isChecked)
            {
                SetAllDescendantsChecked(false);
                UpdateCheckFromChildren();
                return;
            }

            if (!IsRoot)
            {
                SetAllDescendantsChecked(true);
                UpdateCheckFromChildren();
                return;
            }

            var preferredTargets = GetPreferredLocaleTargets();
            if (preferredTargets.Count == 0)
            {
                SetAllDescendantsChecked(true);
                return;
            }

            SetAllDescendantsChecked(false);
            foreach (var target in preferredTargets)
            {
                target.IsChecked = true;
            }

            UpdateCheckFromChildren();
        }

        private void SetAllDescendantsChecked(bool isChecked)
        {
            foreach (var child in ChildrenNodes)
            {
                child.SetCheckedStateRecursive(isChecked);
            }
        }

        private void SetCheckedStateRecursive(bool isChecked)
        {
            if (_isChecked != isChecked)
            {
                _isChecked = isChecked;
                OnPropertyChanged(nameof(IsChecked));
            }

            foreach (var child in ChildrenNodes)
            {
                child.SetCheckedStateRecursive(isChecked);
            }
        }

        private void UpdateCheckFromChildren()
        {
            if (ChildrenNodes.Count == 0)
                return;

            bool hasChecked = false;
            bool hasUnchecked = false;

            foreach (var child in ChildrenNodes)
            {
                if (child.IsChecked == true)
                {
                    hasChecked = true;
                }
                else if (child.IsChecked == false)
                {
                    hasUnchecked = true;
                }
                else
                {
                    hasChecked = true;
                    hasUnchecked = true;
                }

                if (hasChecked && hasUnchecked)
                    break;
            }

            bool? newState = hasChecked && hasUnchecked ? null : (hasChecked ? true : false);
            SetCheckedInternal(newState, propagateToChildren: false, updateParents: false);

            Parent?.UpdateCheckFromChildren();
        }

        private List<TreeNodeItem> GetPreferredLocaleTargets()
        {
            string sourceLanguage = Settings.Default.SourceLanguage;
            var targets = GetLocaleTargets(sourceLanguage);

            if (targets.Count == 0 && !string.Equals(sourceLanguage, "en_us", StringComparison.OrdinalIgnoreCase))
            {
                targets = GetLocaleTargets("en_us");
            }

            return targets;
        }

        private List<TreeNodeItem> GetLocaleTargets(string locale)
        {
            var targets = new HashSet<TreeNodeItem>();

            foreach (var node in EnumerateDescendants(this))
            {
                if (IsLocaleFolder(node, locale))
                {
                    targets.Add(node);
                    foreach (var fileNode in EnumerateDescendants(node))
                    {
                        if (IsLocalizationFile(fileNode.FileName))
                            targets.Add(fileNode);
                    }
                }
                else if (IsLocaleFile(node, locale))
                {
                    targets.Add(node);
                }
            }

            return targets.ToList();
        }

        private static bool IsLocaleFolder(TreeNodeItem node, string locale)
        {
            return node.HasItems &&
                   !string.IsNullOrWhiteSpace(node.FileName) &&
                   node.FileName.Equals(locale, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLocaleFile(TreeNodeItem node, string locale)
        {
            if (string.IsNullOrWhiteSpace(node.FileName))
                return false;

            return IsLocalizationFile(node.FileName) &&
                   node.FileName.Contains(locale, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLocalizationFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".lang", StringComparison.OrdinalIgnoreCase) ||
                   ext.Equals(".snbt", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<TreeNodeItem> EnumerateDescendants(TreeNodeItem node)
        {
            foreach (var child in node.ChildrenNodes)
            {
                yield return child;

                foreach (var grandChild in EnumerateDescendants(child))
                    yield return grandChild;
            }
        }

        private void OnChildrenCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems != null)
            {
                foreach (var item in args.NewItems)
                {
                    if (item is TreeNodeItem child)
                    {
                        child.Parent = this;
                    }
                }
            }

            if (args.OldItems != null)
            {
                foreach (var item in args.OldItems)
                {
                    if (item is TreeNodeItem child && ReferenceEquals(child.Parent, this))
                    {
                        child.Parent = null;
                    }
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private bool _isTranslating;
        public bool IsTranslating
        {
            get => _isTranslating;
            set
            {
                if (_isTranslating != value)
                {
                    _isTranslating = value;
                    OnPropertyChanged(nameof(IsTranslating));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
