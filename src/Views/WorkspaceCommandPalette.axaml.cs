using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class WorkspaceCommandPalette : UserControl
    {
        public WorkspaceCommandPalette()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            FilterTextBox.Focus(NavigationMethod.Directional);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is not ViewModels.WorkspaceCommandPalette vm)
                return;

            if (e.Key == Key.Enter)
            {
                vm.Switch();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (WorkspaceListBox.IsKeyboardFocusWithin)
                {
                    FilterTextBox.Focus(NavigationMethod.Directional);
                    e.Handled = true;
                    return;
                }
            }
            else if (e.Key == Key.Down || e.Key == Key.Tab)
            {
                if (FilterTextBox.IsKeyboardFocusWithin)
                {
                    if (vm.Workspaces.Count > 0)
                        WorkspaceListBox.Focus(NavigationMethod.Directional);

                    e.Handled = true;
                    return;
                }

                if (WorkspaceListBox.IsKeyboardFocusWithin && e.Key == Key.Tab)
                {
                    FilterTextBox.Focus(NavigationMethod.Directional);
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnItemTapped(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.WorkspaceCommandPalette vm)
            {
                vm.Switch();
                e.Handled = true;
            }
        }
    }
}
