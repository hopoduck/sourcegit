# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

SourceGit is a cross-platform Git GUI client built with .NET 10 and Avalonia 11 (MVVM via CommunityToolkit.Mvvm). It shells out to the user's installed `git` (>= 2.25.1) for every operation.

## This fork

- `origin` = `hopoduck/sourcegit` (private personal fork), `upstream` = `sourcegit-scm/sourcegit` (upstream works on `develop`). The base branch here is `master`.
- Fork customizations (Fork-like density, header toolbar, 22px rows, graph palette and ref badges) live almost entirely in `src/Views` and `src/Resources`. Keep new changes in the view layer and touch `Models`/`ViewModels`/`Commands` minimally so upstream merges stay small.
- GitHub Actions are disabled on the fork, so the CI checks below must be run locally. Never push tags.

## Commands

```sh
git submodule update --init                  # required once: depends/AvaloniaEdit is a project reference
dotnet build src/SourceGit.csproj -c Debug   # output: src/bin/Debug/net10.0/SourceGit.exe
dotnet format --verify-no-changes src/SourceGit.csproj   # CI format check (drop --verify-no-changes to fix)
dotnet publish -c Release -r win-x64 -o <dir> src/SourceGit.csproj   # AOT + trimmed; add -p:DisableAOT=true to skip AOT
```

- There is no test project; verify changes by building and running the app.
- The build fails while the Debug exe is running (locked output files). Close the app before rebuilding.
- In a new git worktree the submodule is empty. Populate it cheaply with `git -C <worktree> submodule update --init --reference <main-checkout>/depends/AvaloniaEdit depends/AvaloniaEdit`.
- SourceGit is single-instance: launching a second exe forwards its arguments to the running instance over IPC and exits. To run a dev build next to an installed one, use Portable Mode by creating a `data` folder next to the exe, which gives it separate settings and a separate instance lock. Otherwise settings live in `%APPDATA%\SourceGit` on Windows.
- CLI: `SourceGit <DIR>` opens a repo, `--history <FILE_OR_DIR>` shows file history, `--blame <FILE>` blames a file.

## Architecture

`src/` is split by layer:

- `Commands/`: one class per git operation, deriving from `Commands.Command`. Subclasses set `WorkingDirectory`/`Args`, then call `ExecAsync()` (streams output into an optional `Models.ICommandLog`, raises errors to the UI when `RaiseError`) or `ReadToEndAsync()` (captures stdout/stderr for parsing).
- `Models/`: plain data and parsers (commits, branches, decorators, `CommitGraph` lane layout, `Watcher`, `IpcChannel`, theme overrides).
- `ViewModels/`: UI state. `Launcher` owns the tabs (`LauncherPage`). Each page's `Data` is either `Welcome` or `Repository`. `Repository` is the hub for a single repo: it owns a `Models.Watcher` whose `FileSystemWatcher`s trigger `RefreshBranches`/`RefreshWorkingCopyChanges`/etc. It also hosts the sub-views (`Histories`, `WorkingCopy`, `StashesPage`).
- `Views/`: Avalonia controls. A view is found from its view model **by name**: `ViewModels.Foo` → `Views.Foo`, via `ControlExtensions.CreateFromViewModels` (used for popups, dialogs and the command palette). New popups and windows must follow that naming.
- `Native/`: per-OS integration (`OS.cs` dispatching to `Windows`/`MacOS`/`Linux`) for paths, external tools and shells.
- `AI/`: OpenAI-compatible commit message generation.

Key flows and conventions:

- **Popups**: operations with a form (create branch, push, merge, ...) are `ViewModels.Popup` subclasses (`ObservableValidator`, so DataAnnotations validation works). `Sure()` performs the git work and reports progress through `ProgressDescription`. `CanStartDirectly()` lets a popup skip its form.
- **Commit graph**: `Models.CommitGraph.Generate` assigns lanes and produces `Paths`/`Links`/`Dots` plus each commit's `Color`/`LeftMargin`. `Views.CommitGraph` renders them over the `Histories` DataGrid, and `Views.CommitRefsPresenter` draws the branch/tag badges in each row. Pen colors come from `CommitGraph.SetPens`/`SetDefaultPens`, which a user theme override file can replace.
- **AOT/trimming**: Release builds are `PublishAot` + trimmed. Anything serialized to JSON must be registered with a `[JsonSerializable]` attribute in `App.JsonCodeGen.cs` and serialized through `JsonCodeGen`; avoid new reflection-based APIs.
- **Compiled bindings** are on by default (`AvaloniaUseCompiledBindingsByDefault`). Every template needs an `x:DataType`, and bindings to an ancestor's DataContext need a cast, e.g. `$parent[v:Launcher].((vm:Launcher)DataContext).ActivePage`.
- **Resources**: `Resources/Icons.axaml` (`StreamGeometry` icons, loaded before styles so `{StaticResource Icons.*}` works inside `Styles.axaml`), `Resources/Styles.axaml` (later styles win), and `Resources/Themes.axaml` (per-variant `Color.*`, consumed as `{DynamicResource Brush.*}`).
- **Localization**: `Resources/Locales/en_US.axaml` is the source of truth. Every other locale merges `en_US` as a fallback, so a new key only needs to be added to `en_US` (plus any translations you have). Look up text in code with `App.Text("Key")`, which prepends `Text.`. `python translate_helper.py <lang_id> [--check]` helps fill missing keys, and `node build/scripts/localization-check.js` regenerates `TRANSLATION.md` and sorts the locale files.

## Code style

`.editorconfig` is enforced by the `dotnet format` check: 4-space indent, braces on new lines, `var` preferred, `_camelCase` private/internal fields, `s_camelCase` private static fields, PascalCase constants, and namespaces that match folders. Many existing `.cs` files start with a UTF-8 BOM, so edit them in place instead of rewriting whole files, which keeps the BOM and the diff clean. Line endings are normalized by `.gitattributes` (`text=auto`).
