using System;
using System.Collections.Generic;
using System.IO;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class RepositoryToolbar : UserControl
    {
        public RepositoryToolbar()
        {
            InitializeComponent();
        }

        private void OpenWithExternalTools(object sender, RoutedEventArgs ev)
        {
            if (sender is Button button && DataContext is ViewModels.Repository repo)
            {
                var fullpath = repo.FullPath;
                if (!Directory.Exists(fullpath))
                    return;

                var isMacOS = OperatingSystem.IsMacOS();
                var menu = new ContextMenu();
                menu.Placement = PlacementMode.BottomEdgeAlignedLeft;

                RenderOptions.SetBitmapInterpolationMode(menu, BitmapInterpolationMode.HighQuality);
                RenderOptions.SetEdgeMode(menu, EdgeMode.Antialias);
                RenderOptions.SetTextRenderingMode(menu, TextRenderingMode.SubpixelAntialias);

                var explore = new MenuItem();
                explore.Header = App.Text("Repository.Explore");
                explore.Icon = this.CreateMenuIcon("Icons.Explore");
                explore.Tag = isMacOS ? "⌘+E" : "Ctrl+E";
                explore.Click += (_, e) =>
                {
                    Native.OS.OpenInFileManager(fullpath);
                    e.Handled = true;
                };

                var terminal = new MenuItem();
                terminal.Header = App.Text("Repository.Terminal");
                terminal.Icon = this.CreateMenuIcon("Icons.Terminal");
                terminal.Tag = isMacOS ? "Λ+`" : "Ctrl+`";
                terminal.Click += (_, e) =>
                {
                    Native.OS.OpenTerminal(fullpath);
                    e.Handled = true;
                };

                menu.Items.Add(explore);
                menu.Items.Add(terminal);

                var tools = Native.OS.ExternalTools;
                if (tools.Count > 0)
                {
                    menu.Items.Add(new MenuItem() { Header = "-" });

                    foreach (var tool in tools)
                    {
                        var dupTool = tool;

                        var item = new MenuItem();
                        item.Header = App.Text("Repository.OpenIn", dupTool.Name);
                        item.Icon = new Image { Width = 16, Height = 16, Source = dupTool.IconImage };

                        var options = dupTool.MakeLaunchOptions(fullpath);
                        var count = (dupTool.SupportOpenFolder ? 1 : 0) + (options?.Count ?? 0);
                        if (count == 0)
                            continue;

                        if (count == 1)
                        {
                            var args = fullpath.Quoted();
                            if (options is { Count: 1 })
                                args = options[0].Args;

                            item.Click += (_, e) =>
                            {
                                dupTool.Launch(args);
                                e.Handled = true;
                            };
                        }
                        else
                        {
                            foreach (var opt in options)
                            {
                                var subItem = new MenuItem();
                                subItem.Header = opt.Title;
                                subItem.Click += (_, e) =>
                                {
                                    dupTool.Launch(opt.Args);
                                    e.Handled = true;
                                };

                                item.Items.Add(subItem);
                            }

                            if (dupTool.SupportOpenFolder)
                            {
                                var open = new MenuItem();
                                open.Header = App.Text("Repository.OpenAsFolder");
                                open.Click += (_, e) =>
                                {
                                    dupTool.Launch(fullpath.Quoted());
                                    e.Handled = true;
                                };
                                item.Items.Add(new MenuItem() { Header = "-" });
                                item.Items.Add(open);
                            }
                        }

                        menu.Items.Add(item);
                    }
                }

                var urls = new Dictionary<string, string>();
                foreach (var r in repo.Remotes)
                {
                    if (r.TryGetVisitURL(out var visit))
                        urls.Add(r.Name, visit);
                }

                if (urls.Count > 0)
                {
                    menu.Items.Add(new MenuItem() { Header = "-" });

                    foreach (var (name, addr) in urls)
                    {
                        var dupUrl = addr;

                        var item = new MenuItem();
                        item.Header = App.Text("Repository.Visit", name);
                        item.Icon = this.CreateMenuIcon("Icons.Remotes");
                        item.Click += (_, e) =>
                        {
                            Native.OS.OpenBrowser(dupUrl);
                            e.Handled = true;
                        };

                        menu.Items.Add(item);
                    }
                }

                menu.Open(button);
                ev.Handled = true;
            }
        }

        private async void OpenStatistics(object _, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await this.ShowDialogAsync(new ViewModels.Statistics(repo.FullPath));
                e.Handled = true;
            }
        }

        private async void Fetch(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await repo.FetchAsync(e.KeyModifiers is KeyModifiers.Control);
                e.Handled = true;
            }
        }

        private async void FetchByHotKey(object sender, RoutedEventArgs e)
        {
            if (App.GetLauncher() is { CommandPalette: { } } launcher)
                return;

            if (DataContext is ViewModels.Repository repo)
            {
                await repo.FetchAsync(false);
                e.Handled = true;
            }
        }

        private async void FetchDirectlyByHotKey(object sender, RoutedEventArgs e)
        {
            if (App.GetLauncher() is { CommandPalette: { } } launcher)
                return;

            if (DataContext is ViewModels.Repository repo)
            {
                await repo.FetchAsync(true);
                e.Handled = true;
            }
        }

        private async void Pull(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await repo.PullAsync(e.KeyModifiers is KeyModifiers.Control);
                e.Handled = true;
            }
        }

        private async void PullByHotKey(object sender, RoutedEventArgs e)
        {
            if (App.GetLauncher() is { CommandPalette: { } } launcher)
                return;

            if (DataContext is ViewModels.Repository repo)
            {
                await repo.PullAsync(false);
                e.Handled = true;
            }
        }

        private async void PullDirectlyByHotKey(object sender, RoutedEventArgs e)
        {
            if (App.GetLauncher() is { CommandPalette: { } } launcher)
                return;

            if (DataContext is ViewModels.Repository repo)
            {
                await repo.PullAsync(true);
                e.Handled = true;
            }
        }

        private async void Push(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await repo.PushAsync(e.KeyModifiers is KeyModifiers.Control);
                e.Handled = true;
            }
        }

        private async void PushByHotKey(object sender, RoutedEventArgs e)
        {
            if (App.GetLauncher() is { CommandPalette: { } } launcher)
                return;

            if (DataContext is ViewModels.Repository repo)
            {
                await repo.PushAsync(false);
                e.Handled = true;
            }
        }

        private async void PushDirectlyByHotKey(object sender, RoutedEventArgs e)
        {
            if (App.GetLauncher() is { CommandPalette: { } } launcher)
                return;

            if (DataContext is ViewModels.Repository repo)
            {
                await repo.PushAsync(true);
                e.Handled = true;
            }
        }

        private void FillGitFlowMenu(ItemCollection items, ViewModels.Repository repo)
        {
            if (repo.IsGitFlowEnabled())
            {
                var startFeature = new MenuItem();
                startFeature.Header = App.Text("GitFlow.StartFeature");
                startFeature.Click += (_, e) =>
                {
                    if (repo.CanCreatePopup())
                        repo.ShowPopup(new ViewModels.GitFlowStart(repo, Models.GitFlowBranchType.Feature));
                    e.Handled = true;
                };

                var startRelease = new MenuItem();
                startRelease.Header = App.Text("GitFlow.StartRelease");
                startRelease.Click += (_, e) =>
                {
                    if (repo.CanCreatePopup())
                        repo.ShowPopup(new ViewModels.GitFlowStart(repo, Models.GitFlowBranchType.Release));
                    e.Handled = true;
                };

                var startHotfix = new MenuItem();
                startHotfix.Header = App.Text("GitFlow.StartHotfix");
                startHotfix.Click += (_, e) =>
                {
                    if (repo.CanCreatePopup())
                        repo.ShowPopup(new ViewModels.GitFlowStart(repo, Models.GitFlowBranchType.Hotfix));
                    e.Handled = true;
                };

                items.Add(startFeature);
                items.Add(startRelease);
                items.Add(startHotfix);

                var type = repo.CurrentBranch != null ? repo.GetGitFlowType(repo.CurrentBranch) : Models.GitFlowBranchType.None;
                if (type != Models.GitFlowBranchType.None)
                {
                    var finish = new MenuItem();
                    finish.Header = App.Text("GitFlow.Finish", repo.CurrentBranch.Name);
                    finish.Icon = this.CreateMenuIcon("Icons.GitFlow.Finish");
                    finish.Click += (_, e) =>
                    {
                        if (repo.CanCreatePopup())
                            repo.ShowPopup(new ViewModels.GitFlowFinish(repo, repo.CurrentBranch, type));
                        e.Handled = true;
                    };
                    items.Add(new MenuItem() { Header = "-" });
                    items.Add(finish);
                }
            }
            else
            {
                var init = new MenuItem();
                init.Header = App.Text("GitFlow.Init");
                init.Icon = this.CreateMenuIcon("Icons.Init");
                init.Click += (_, e) =>
                {
                    if (repo.CurrentBranch == null)
                        repo.SendNotification("Git flow init failed: No branch found!!!", true);
                    else if (repo.CanCreatePopup())
                        repo.ShowPopup(new ViewModels.InitGitFlow(repo));

                    e.Handled = true;
                };
                items.Add(init);
            }
        }

        private void FillGitLFSMenu(ItemCollection items, ViewModels.Repository repo)
        {
            if (repo.IsLFSEnabled())
            {
                var addPattern = new MenuItem();
                addPattern.Header = App.Text("GitLFS.AddTrackPattern");
                addPattern.Icon = this.CreateMenuIcon("Icons.File.Add");
                addPattern.Click += (_, e) =>
                {
                    if (repo.CanCreatePopup())
                        repo.ShowPopup(new ViewModels.LFSTrackCustomPattern(repo));

                    e.Handled = true;
                };
                items.Add(addPattern);
                items.Add(new MenuItem() { Header = "-" });

                var fetch = new MenuItem();
                fetch.Header = App.Text("GitLFS.Fetch");
                fetch.Icon = this.CreateMenuIcon("Icons.Fetch");
                fetch.IsEnabled = repo.Remotes.Count > 0;
                fetch.Click += async (_, e) =>
                {
                    if (repo.CanCreatePopup())
                    {
                        if (repo.Remotes.Count == 1)
                            await repo.ShowAndStartPopupAsync(new ViewModels.LFSFetch(repo));
                        else
                            repo.ShowPopup(new ViewModels.LFSFetch(repo));
                    }

                    e.Handled = true;
                };
                items.Add(fetch);

                var pull = new MenuItem();
                pull.Header = App.Text("GitLFS.Pull");
                pull.Icon = this.CreateMenuIcon("Icons.Pull");
                pull.IsEnabled = repo.Remotes.Count > 0;
                pull.Click += async (_, e) =>
                {
                    if (repo.CanCreatePopup())
                    {
                        if (repo.Remotes.Count == 1)
                            await repo.ShowAndStartPopupAsync(new ViewModels.LFSPull(repo));
                        else
                            repo.ShowPopup(new ViewModels.LFSPull(repo));
                    }

                    e.Handled = true;
                };
                items.Add(pull);

                var push = new MenuItem();
                push.Header = App.Text("GitLFS.Push");
                push.Icon = this.CreateMenuIcon("Icons.Push");
                push.IsEnabled = repo.Remotes.Count > 0;
                push.Click += async (_, e) =>
                {
                    if (repo.CanCreatePopup())
                    {
                        if (repo.Remotes.Count == 1)
                            await repo.ShowAndStartPopupAsync(new ViewModels.LFSPush(repo));
                        else
                            repo.ShowPopup(new ViewModels.LFSPush(repo));
                    }

                    e.Handled = true;
                };
                items.Add(push);

                var prune = new MenuItem();
                prune.Header = App.Text("GitLFS.Prune");
                prune.Icon = this.CreateMenuIcon("Icons.Clean");
                prune.Click += async (_, e) =>
                {
                    if (repo.CanCreatePopup())
                        await repo.ShowAndStartPopupAsync(new ViewModels.LFSPrune(repo));

                    e.Handled = true;
                };
                items.Add(new MenuItem() { Header = "-" });
                items.Add(prune);

                var locks = new MenuItem();
                locks.Header = App.Text("GitLFS.Locks");
                locks.Icon = this.CreateMenuIcon("Icons.Lock");
                locks.IsEnabled = repo.Remotes.Count > 0;
                if (repo.Remotes.Count == 1)
                {
                    locks.Click += async (_, e) =>
                    {
                        await this.ShowDialogAsync(new ViewModels.LFSLocks(repo, repo.Remotes[0].Name));
                        e.Handled = true;
                    };
                }
                else
                {
                    foreach (var remote in repo.Remotes)
                    {
                        var remoteName = remote.Name;
                        var lockRemote = new MenuItem();
                        lockRemote.Header = remoteName;
                        lockRemote.Click += async (_, e) =>
                        {
                            await this.ShowDialogAsync(new ViewModels.LFSLocks(repo, remoteName));
                            e.Handled = true;
                        };
                        locks.Items.Add(lockRemote);
                    }
                }

                items.Add(new MenuItem() { Header = "-" });
                items.Add(locks);
            }
            else
            {
                var install = new MenuItem();
                install.Header = App.Text("GitLFS.Install");
                install.Icon = this.CreateMenuIcon("Icons.Init");
                install.Click += async (_, e) =>
                {
                    await repo.InstallLFSAsync();
                    e.Handled = true;
                };
                items.Add(install);
            }
        }

        private async void StartBisect(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository { IsBisectCommandRunning: false, InProgressContext: null } repo &&
                repo.CanCreatePopup())
            {
                if (repo.LocalChangesCount > 0)
                    repo.SendNotification("You have un-committed local changes. Please discard or stash them first.", true);
                else if (repo.IsBisectCommandRunning || repo.BisectState != Models.BisectState.None)
                    repo.SendNotification("Bisect is running! Please abort it before starting a new one.", true);
                else
                    await repo.ExecBisectCommandAsync("start");
            }

            e.Handled = true;
        }

        private void FillCustomActionMenu(ItemCollection items, ViewModels.Repository repo)
        {
            var actions = repo.GetCustomActions(Models.CustomActionScope.Repository);
            if (actions.Count > 0)
            {
                foreach (var action in actions)
                {
                    var (dup, label) = action;
                    var item = new MenuItem();
                    item.Icon = this.CreateMenuIcon("Icons.Action");
                    item.Header = label;
                    item.Click += async (_, e) =>
                    {
                        await repo.ExecCustomActionAsync(dup, null);
                        e.Handled = true;
                    };

                    items.Add(item);
                }
            }
            else
            {
                items.Add(new MenuItem() { Header = App.Text("Repository.CustomActions.Empty") });
            }
        }

        private void OpenMoreMenu(object sender, RoutedEventArgs ev)
        {
            if (DataContext is ViewModels.Repository repo && sender is Control control)
            {
                var menu = new ContextMenu();
                menu.Placement = PlacementMode.BottomEdgeAlignedLeft;

                if (!repo.IsBare)
                {
                    var stash = new MenuItem();
                    stash.Header = App.Text("Stash");
                    stash.Icon = this.CreateMenuIcon("Icons.Stashes.Add");
                    stash.Click += async (_, e) =>
                    {
                        await repo.StashAllAsync(false);
                        e.Handled = true;
                    };
                    menu.Items.Add(stash);

                    var patch = new MenuItem();
                    patch.Header = App.Text("Apply.Title");
                    patch.Icon = this.CreateMenuIcon("Icons.ApplyPatch");
                    patch.Click += (_, e) =>
                    {
                        repo.ApplyPatch();
                        e.Handled = true;
                    };
                    menu.Items.Add(patch);
                    menu.Items.Add(new MenuItem() { Header = "-" });
                }

                var logs = new MenuItem();
                logs.Header = App.Text("Repository.ViewLogs");
                logs.Icon = this.CreateMenuIcon("Icons.Logs");
                logs.Click += OpenGitLogs;
                menu.Items.Add(logs);

                var statistics = new MenuItem();
                statistics.Header = App.Text("Repository.Statistics");
                statistics.Icon = this.CreateMenuIcon("Icons.Statistics");
                statistics.Click += OpenStatistics;
                menu.Items.Add(statistics);
                menu.Items.Add(new MenuItem() { Header = "-" });

                if (!repo.IsBare)
                {
                    var gitFlow = new MenuItem();
                    gitFlow.Header = App.Text("GitFlow");
                    gitFlow.Icon = this.CreateMenuIcon("Icons.GitFlow");
                    FillGitFlowMenu(gitFlow.Items, repo);
                    menu.Items.Add(gitFlow);

                    var lfs = new MenuItem();
                    lfs.Header = App.Text("GitLFS");
                    lfs.Icon = this.CreateMenuIcon("Icons.LFS");
                    FillGitLFSMenu(lfs.Items, repo);
                    menu.Items.Add(lfs);

                    var bisect = new MenuItem();
                    bisect.Header = App.Text("Bisect");
                    bisect.Icon = this.CreateMenuIcon("Icons.Bisect");
                    bisect.Click += StartBisect;
                    menu.Items.Add(bisect);
                }

                var customActions = new MenuItem();
                customActions.Header = App.Text("Repository.CustomActions");
                customActions.Icon = this.CreateMenuIcon("Icons.Action");
                FillCustomActionMenu(customActions.Items, repo);
                menu.Items.Add(customActions);
                menu.Items.Add(new MenuItem() { Header = "-" });

                if (!repo.IsBare)
                {
                    var addSubmodule = new MenuItem();
                    addSubmodule.Header = App.Text("Repository.Submodules.Add");
                    addSubmodule.Icon = this.CreateMenuIcon("Icons.Submodule.Add");
                    addSubmodule.Click += (_, e) =>
                    {
                        repo.AddSubmodule();
                        e.Handled = true;
                    };
                    menu.Items.Add(addSubmodule);
                }

                var addWorktree = new MenuItem();
                addWorktree.Header = App.Text("Repository.Worktrees.Add");
                addWorktree.Icon = this.CreateMenuIcon("Icons.Worktree.Add");
                addWorktree.Click += (_, e) =>
                {
                    repo.AddWorktree();
                    e.Handled = true;
                };
                menu.Items.Add(addWorktree);
                menu.Items.Add(new MenuItem() { Header = "-" });

                var cleanup = new MenuItem();
                cleanup.Header = App.Text("Repository.Clean");
                cleanup.Icon = this.CreateMenuIcon("Icons.Clean");
                cleanup.Click += Cleanup;
                menu.Items.Add(cleanup);

                menu.Open(control);
            }

            ev.Handled = true;
        }

        private async void OpenGitLogs(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await this.ShowDialogAsync(new ViewModels.ViewLogs(repo));
                e.Handled = true;
            }
        }

        private async void Cleanup(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
            {
                await repo.CleanupAsync();
                e.Handled = true;
            }
        }

        private void NavigateToHead(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository { CurrentBranch: { } head } repo)
            {
                var repoView = TopLevel.GetTopLevel(this)?.FindDescendantOfType<Repository>();
                repoView?.LocalBranchTree?.Select(head);

                repo.NavigateToCommit(head.Head);
                e.Handled = true;
            }
        }
    }
}
