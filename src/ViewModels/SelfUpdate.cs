using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class SelfUpdate : ObservableObject
    {
        public object Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            private set => SetProperty(ref _isInstalling, value);
        }

        public double InstallProgress
        {
            get => _installProgress;
            private set => SetProperty(ref _installProgress, value);
        }

        public string InstallError
        {
            get => _installError;
            private set => SetProperty(ref _installError, value);
        }

        /// <summary>
        ///     Downloads the package and hands it to the install script. Returns true when the app must quit
        ///     so the script can replace its files.
        /// </summary>
        public async Task<bool> InstallAsync(Models.Version ver)
        {
            if (ver.InstallPackage is not { } asset || IsInstalling)
                return false;

            IsInstalling = true;
            InstallProgress = 0;
            InstallError = null;

            try
            {
                var packageDir = await Models.SelfUpdateInstaller.DownloadAsync(
                    asset,
                    p => Dispatcher.UIThread.Post(() => InstallProgress = p));

                if (Models.SelfUpdateInstaller.StartInstall(packageDir))
                    return true;
            }
            catch (Exception e)
            {
                InstallError = e.InnerException?.Message ?? e.Message;
            }

            IsInstalling = false;
            return false;
        }

        private object _data = null;
        private bool _isInstalling = false;
        private double _installProgress = 0;
        private string _installError = null;
    }
}
