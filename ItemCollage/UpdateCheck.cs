using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ItemCollage
{
    public static class UpdateCheck
    {
        private static readonly HttpClient httpClient = new HttpClient();

        const string UpdateUrl = "https://raw.github.com/ygra/Diablo3ItemCollage/master/version";
        const string ChangelogUrl = "https://raw.github.com/ygra/Diablo3ItemCollage/master/changelog";
        const string DownloadUrl = "https://github.com/ygra/Diablo3ItemCollage/downloads";


        public static async Task<(bool UpdateAvailable, Version newVersion, string Changelog)> IsUpdateAvailable()
        {
            var result = await httpClient.GetStringAsync(UpdateUrl);

            var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var remoteVersion = new Version(result);

            if (remoteVersion > localVersion)
            {
                return (true, remoteVersion, await GetChangelog(localVersion, remoteVersion));
            }
            return (false, new Version(), null);
        }

        public static async Task<string> GetChangelog(Version oldVersion, Version newVersion)
        {
            var completeChangelog = await httpClient.GetStringAsync(ChangelogUrl);

            var lines = Regex.Split(completeChangelog, "\r?\n");

            var versionLine = new Regex(@"^\d{4}-\d{2}-\d{2}\tv(?<version>[\d.]+)");
            var log = new List<string>();
            var capture = false;
            foreach (var line in lines)
            {
                // did we hit a line that starts the changelog for a specific version?
                Match m = versionLine.Match(line);
                if (m.Success)
                {
                    var lineVersion = new Version(m.Groups["version"].Value);
                    // capture only newer versions
                    if (lineVersion <= newVersion) capture = true;
                    // and leave the older ones out
                    if (lineVersion <= oldVersion) capture = false;
                }

                // grab line only if it doesn't start with a . which marks minor changes
                if (capture && !line.StartsWith("."))
                {
                    // make the asterisk into a nice bullet point
                    var lineToAdd = Regex.Replace(line, @"^\*", "     •");
                    // remove the date from the version line
                    lineToAdd = Regex.Replace(lineToAdd, @"^[\d-]+\t", "");

                    log.Add(lineToAdd);
                }
            }

            return string.Join("\r\n", log.ToArray());
        }

        public static void OpenDownloadUrl()
        {
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = DownloadUrl
            });
        }
    }
}