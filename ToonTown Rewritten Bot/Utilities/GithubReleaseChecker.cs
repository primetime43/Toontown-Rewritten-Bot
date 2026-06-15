using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot.Utilities
{
    public static class GithubReleaseChecker
    {
        private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
        private const string repoOwner = "primetime43";
        private const string repoName = "Toontown-Rewritten-Bot";
        private const string userAgent = "request";

        static GithubReleaseChecker()
        {
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }
        }

        public static async Task CheckForNewVersion()
        {
            string url = $"repos/{repoOwner}/{repoName}/releases/latest";
            try
            {
                var release = await client.GetFromJsonAsync<Release>(url);
                if (release != null)
                {
                    CompareAndPrompt(release);
                }
                else
                {
                    MessageBox.Show("No new releases found.", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check for updates: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CompareAndPrompt(Release release)
        {
            string currentVersion = GlobalSettings.ApplicationInfo.Version;

            // Only prompt when the published release is strictly newer than the
            // running version. A plain string comparison would flag any mismatch,
            // including older releases when running an unpublished newer build.
            if (TryParseVersion(release.tag_name, out var latest) &&
                TryParseVersion(currentVersion, out var current) &&
                latest > current)
            {
                var result = MessageBox.Show($"New version available: {release.tag_name}\nDo you want to download it?",
                    "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"https://github.com/{repoOwner}/{repoName}/releases/latest",
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                Debug.WriteLine("You are already using the latest version.", "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Tolerate tags like "v2.5.0" or "2.5.0-beta" by trimming a leading
            // "v" and any suffix after the numeric portion.
            string trimmed = value.Trim();
            if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(1);
            }

            int suffixIndex = trimmed.IndexOfAny(new[] { '-', '+', ' ' });
            if (suffixIndex >= 0)
            {
                trimmed = trimmed.Substring(0, suffixIndex);
            }

            return Version.TryParse(trimmed, out version);
        }

        public class Release
        {
            public string tag_name { get; set; }
            public string name { get; set; }
            public DateTime published_at { get; set; }
        }
    }
}