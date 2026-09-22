using System;
using System.Collections.Generic;

namespace SourceGit.ViewModels
{
    public class WorkspaceCommandPalette : ICommandPalette
    {
        public List<Workspace> Workspaces
        {
            get => _workspaces;
            private set => SetProperty(ref _workspaces, value);
        }

        public Workspace SelectedWorkspace
        {
            get => _selectedWorkspace;
            set => SetProperty(ref _selectedWorkspace, value);
        }

        public string Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                    UpdateWorkspaces();
            }
        }

        public WorkspaceCommandPalette(Launcher launcher)
        {
            _launcher = launcher;
            UpdateWorkspaces();
        }

        public void ClearFilter()
        {
            Filter = string.Empty;
        }

        public void Switch()
        {
            _workspaces.Clear();
            Close();

            if (_selectedWorkspace != null)
                _launcher.SwitchWorkspace(_selectedWorkspace);
        }

        private void UpdateWorkspaces()
        {
            var workspaces = new List<Workspace>();
            foreach (var w in Preferences.Instance.Workspaces)
            {
                if (w.IsActive)
                    continue;

                if (string.IsNullOrEmpty(_filter) || w.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    workspaces.Add(w);
            }

            var autoSelected = _selectedWorkspace;
            if (workspaces.Count == 0)
                autoSelected = null;
            else if (_selectedWorkspace == null || !workspaces.Contains(_selectedWorkspace))
                autoSelected = workspaces[0];

            Workspaces = workspaces;
            SelectedWorkspace = autoSelected;
        }

        private Launcher _launcher = null;
        private List<Workspace> _workspaces = [];
        private Workspace _selectedWorkspace = null;
        private string _filter;
    }
}
