using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SourceGit.Models
{
    /// <summary>
    ///     Replaces the running Windows build with a downloaded release package. The files of a running
    ///     exe cannot be overwritten, so a detached PowerShell script waits for this process to exit,
    ///     copies the package over the install folder and starts the new version.
    /// </summary>
    public static class SelfUpdateInstaller
    {
        public static bool IsSupported =>
            OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64;

        private static string WorkDir => Path.Combine(Path.GetTempPath(), "sourcegit-update");

        public static async Task<string> DownloadAsync(ReleaseAsset asset, Action<double> onProgress)
        {
            var workDir = WorkDir;
            if (Directory.Exists(workDir))
                Directory.Delete(workDir, true);
            Directory.CreateDirectory(workDir);

            var zipFile = Path.Combine(workDir, asset.Name);
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SourceGit");

                using var rsp = await client.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                rsp.EnsureSuccessStatusCode();

                var total = rsp.Content.Headers.ContentLength ?? asset.Size;
                await using var input = await rsp.Content.ReadAsStreamAsync();
                await using var output = File.Create(zipFile);

                var buffer = new byte[81920];
                var received = 0L;
                int read;
                while ((read = await input.ReadAsync(buffer)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read));
                    received += read;
                    if (total > 0)
                        onProgress?.Invoke(received * 100.0 / total);
                }
            }

            var extractDir = Path.Combine(workDir, "package");
            await Task.Run(() => ZipFile.ExtractToDirectory(zipFile, extractDir));

            // The package zips the whole 'SourceGit' folder, so the files live one level down.
            var packageDir = Path.Combine(extractDir, "SourceGit");
            if (!File.Exists(Path.Combine(packageDir, "SourceGit.exe")))
                packageDir = extractDir;
            if (!File.Exists(Path.Combine(packageDir, "SourceGit.exe")))
                throw new InvalidDataException($"SourceGit.exe is missing from {asset.Name}");

            return packageDir;
        }

        /// <summary>
        ///     Starts the script that installs the package once this process exits. Returns false when the
        ///     user declines the UAC prompt, which is only shown if the install folder is not writable.
        /// </summary>
        public static bool StartInstall(string packageDir)
        {
            var workDir = WorkDir;
            var script = Path.Combine(workDir, "install.ps1");
            File.WriteAllText(script, SCRIPT, Encoding.UTF8);

            // A trailing backslash would escape the closing quote of the argument.
            var target = AppContext.BaseDirectory.TrimEnd('\\');
            var starter = new ProcessStartInfo("powershell.exe")
            {
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\" " +
                            $"-ProcessId {Environment.ProcessId} -Source \"{packageDir.TrimEnd('\\')}\" " +
                            $"-Target \"{target}\" -WorkDir \"{workDir}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            if (!CanWrite(target))
                starter.Verb = "runas";

            try
            {
                Process.Start(starter);
                return true;
            }
            catch (Win32Exception e) when (e.NativeErrorCode == 1223) // ERROR_CANCELLED
            {
                return false;
            }
        }

        private static bool CanWrite(string dir)
        {
            try
            {
                var probe = Path.Combine(dir, $".update-probe-{Environment.ProcessId}");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // robocopy without /PURGE keeps files that are not in the package, such as the portable 'data' folder.
        // explorer.exe starts the new version with the user's normal token even if this script is elevated.
        private const string SCRIPT = """
            param([int]$ProcessId, [string]$Source, [string]$Target, [string]$WorkDir)

            Wait-Process -Id $ProcessId -Timeout 60 -ErrorAction SilentlyContinue

            robocopy $Source $Target /E /R:10 /W:1 /NP /NFL /NDL /NJH /NJS | Out-Null
            if ($LASTEXITCODE -ge 8) {
                Add-Type -AssemblyName System.Windows.Forms
                [System.Windows.Forms.MessageBox]::Show("Failed to install the update (robocopy exit code $LASTEXITCODE).`n`nPackage: $Source", 'SourceGit') | Out-Null
                exit 1
            }

            Start-Process explorer.exe -ArgumentList ('"' + (Join-Path $Target 'SourceGit.exe') + '"')
            Remove-Item -LiteralPath $WorkDir -Recurse -Force -ErrorAction SilentlyContinue
            """;
    }
}
