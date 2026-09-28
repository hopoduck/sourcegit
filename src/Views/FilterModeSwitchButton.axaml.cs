using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class FilterModeSwitchButton : UserControl
    {
        public static readonly DirectProperty<FilterModeSwitchButton, Models.FilterMode> ModeProperty =
            AvaloniaProperty.RegisterDirect<FilterModeSwitchButton, Models.FilterMode>(
                nameof(Mode),
                static o => o.Mode,
                static (o, v) => o.Mode = v);

        public Models.FilterMode Mode
        {
            get => _mode;
            set => SetAndRaise(ModeProperty, ref _mode, value);
        }

        public static readonly StyledProperty<bool> IsHoverParentProperty =
            AvaloniaProperty.Register<FilterModeSwitchButton, bool>(nameof(IsHoverParent));

        public bool IsHoverParent
        {
            get => GetValue(IsHoverParentProperty);
            set => SetValue(IsHoverParentProperty, value);
        }

        public FilterModeSwitchButton()
        {
            InitializeComponent();
            UpdateButtons();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ModeProperty || change.Property == IsHoverParentProperty)
                UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (IncludeButton == null || ExcludeButton == null)
                return;

            var included = Mode == Models.FilterMode.Included;
            var excluded = Mode == Models.FilterMode.Excluded;

            IncludeButton.IsVisible = included || IsHoverParent;
            ExcludeButton.IsVisible = excluded || IsHoverParent;
            IncludeIcon.Classes.Set("active", included);
            ExcludeIcon.Classes.Set("active", excluded);

            SetCurrentValue(IsVisibleProperty, IncludeButton.IsVisible || ExcludeButton.IsVisible);
        }

        private void OnIncludeButtonClicked(object sender, RoutedEventArgs e)
        {
            ToggleFilterMode(Models.FilterMode.Included);
            e.Handled = true;
        }

        private void OnExcludeButtonClicked(object sender, RoutedEventArgs e)
        {
            ToggleFilterMode(Models.FilterMode.Excluded);
            e.Handled = true;
        }

        private void ToggleFilterMode(Models.FilterMode mode)
        {
            var repoView = this.FindAncestorOfType<Repository>();
            if (repoView?.DataContext is not ViewModels.Repository repo)
                return;

            var target = Mode == mode ? Models.FilterMode.None : mode;
            if (DataContext is ViewModels.TagListItem tagItem)
                repo.SetTagFilterMode(tagItem.Tag, target);
            else if (DataContext is ViewModels.TagTreeNode tagNode)
                repo.SetTagFilterMode(tagNode.Tag, target);
            else if (DataContext is ViewModels.BranchTreeNode branchNode)
                repo.SetBranchFilterMode(branchNode, target, false, true);
        }

        private Models.FilterMode _mode = Models.FilterMode.None;
    }
}
