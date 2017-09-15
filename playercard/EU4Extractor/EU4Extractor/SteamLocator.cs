using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EU4Extractor
{
    internal static class SteamLocator
    {
        private static Regex steamLibraryRegex = new Regex("(?<=BaseInstallFolder_\\d+\"\\t\\t\").+(?=\"$)");

        public static string GetSteamFolder()
        {
            try
            {
                var steam = Registry.CurrentUser?.OpenSubKey("Software")?.OpenSubKey("Valve")?.OpenSubKey("Steam");

                return steam?.GetValue("SteamPath", "")?.ToString().Replace('/', Path.DirectorySeparatorChar);
            }
            catch
            {
                return null;
            }
        }

        public static IEnumerable<string> GetSteamLibraryFolders()
        {
            var steamPath = GetSteamFolder();

            if (steamPath == null)
                yield break;

            yield return Path.Combine(steamPath, "steamapps", "common");

            var config = File.ReadAllLines(Path.Combine(steamPath, "config", "config.vdf"));

            foreach (var line in config)
                if (steamLibraryRegex.IsMatch(line))
                    yield return Path.Combine(steamLibraryRegex.Match(line).Value, "steamapps", "common");
        }

        public static bool HasGame(string game, out string path)
        {
            foreach (var library in GetSteamLibraryFolders())
            {
                path = Path.Combine(library, game);

                if (Directory.Exists(path))
                    return true;
            }

            path = null;
            return false;
        }
    }
}