using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace Protagoniste.Launcher
{
    internal static class Program
    {
        const int UnexpectedCompareResult = -2;
        const int NoLocalNorOnlineVersionAvailable = -1;
        const int ApplicationStartedAndUpToDate = 0;
        const int ApplicationStartedAndUpdated = 1;
        const int ApplicationStartedAndCannotBeUpdated = 2;

        const string DirectoryName = "Protagoniste.App";
        const string AppName = "Protagoniste.exe";

        const string GitHubProductName = "Protagoniste.Launcher";
        const string GitHubAppOwner = "dbraillon";
        const string GitHubRepoName = "Protagoniste";
        const string GitHubAppAssetName = "Protagoniste.zip";

        static readonly string CurrentAppPath = Assembly.GetEntryAssembly().Location;
        static readonly string CurrentDirectoryPath = Path.GetDirectoryName(CurrentAppPath);
        static readonly string AppDirectoryPath = Path.Combine(CurrentDirectoryPath, DirectoryName);
        static readonly string AppPath = Path.Combine(AppDirectoryPath, AppName);

        static Release LatestRelease { get; set; }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        /// <summary>
        /// Get local version of the application.
        /// </summary>
        static Version GetLocalVersion()
        {
            if (!File.Exists(AppPath)) return default;

            var localVersion = FileVersionInfo.GetVersionInfo(AppPath);
            var localVersionStr = localVersion.FileVersion;
            if (localVersionStr is null) return default;

            return new Version(localVersionStr);
        }

        /// <summary>
        /// Get online version of the application.
        /// </summary>
        static async Task<Version> GetOnlineVersion()
        {
            var client = new GitHubClient(new ProductHeaderValue(GitHubProductName));

            LatestRelease = await client.Repository.Release.GetLatest(GitHubAppOwner, GitHubRepoName);
            if (LatestRelease is null) return default;

            var versionStr = LatestRelease.TagName.Trim('v');
            if (!Version.TryParse(versionStr, out Version version)) return default;

            return version;
        }

        /// <summary>
        /// Download online application and start it.
        /// </summary>
        static int DownloadAndStartApplication()
        {
            // TODO: Add a loading view
            //System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.SystemAware);
            //System.Windows.Forms.Application.EnableVisualStyles();
            //System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            //System.Windows.Forms.Application.Run(new CheckUpdateForm());

            DownloadApplication();
            StartApplication();

            return ApplicationStartedAndUpdated;
        }

        /// <summary>
        /// Download online application.
        /// </summary>
        static void DownloadApplication()
        {
            var bundledAppAsset = LatestRelease.Assets.FirstOrDefault(asset => asset.Name == GitHubAppAssetName);
            if (bundledAppAsset is null) return;

            var downloadUrl = bundledAppAsset.BrowserDownloadUrl;
            var tempFilePath = Path.GetTempFileName();

            using var client = new WebClient();
            client.DownloadFile(downloadUrl, tempFilePath);

            if (Directory.Exists(AppDirectoryPath))
                Directory.Delete(AppDirectoryPath, recursive: true);

            ZipFile.ExtractToDirectory(tempFilePath, AppDirectoryPath);
            File.Delete(tempFilePath);
        }

        /// <summary>
        /// Start the local application.
        /// </summary>
        static int StartApplication()
        {
            Process.Start(AppPath);

            return ApplicationStartedAndUpToDate;
        }

        /// <summary>
        /// Ask the user if the application can be updated.
        /// </summary>
        static int AskToUpdate()
        {
            var result = MessageBox.Show("A brand new version of the application is available, would you like to update?", "That's important", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            return result == DialogResult.Yes ?
                DownloadAndStartApplication() :
                StartApplication();
        }
    }
}
