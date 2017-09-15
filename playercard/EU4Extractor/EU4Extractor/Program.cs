using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EU4Extractor
{
    internal class Program
    {
        private static int flagSize = 128;
        private static Regex ymlDictRegex = new Regex(" ([A-Z]{3}):0 \"(.+)\"");

        private static void exit()
        {
            Console.ReadLine();
            Environment.Exit(0);
        }

        private static void Main(string[] args)
        {
            if (!SteamLocator.HasGame("Europa Universalis IV", out var path))
            {
                Console.WriteLine("No EU4 installation found.");
                exit();
            }

            Console.WriteLine($"Found EU4 installation at {path}");
            Console.WriteLine("Reading Localisation...");

            var dict = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(Path.Combine(path, "localisation", "countries_l_english.yml"))
                .Concat(File.ReadAllLines(Path.Combine(path, "localisation", "tags_phase4_l_english.yml"))))
            {
                var match = ymlDictRegex.Match(line);
                if (!match.Success)
                    continue;

                dict.Add(match.Groups[1].Value, match.Groups[2].Value);
            }

            Console.WriteLine("Reading and stitching flags...");

            var flagsPath = Path.Combine(path, "gfx", "flags");
            var flags = new SortedSet<string>(Directory.EnumerateFiles(flagsPath, "*.tga", SearchOption.TopDirectoryOnly)
                .Select(p => Path.GetFileNameWithoutExtension(p)), new DictComparer(dict));
            var atlasSize = (int)Math.Ceiling(Math.Sqrt(flags.Count));

            var atlasDict = new List<string>();
            using (var flagAtlas = new Bitmap(atlasSize * flagSize, atlasSize * flagSize))
            using (var flagAtlasG = Graphics.FromImage(flagAtlas))
            {
                var i = 0;
                foreach (var flag in flags)
                {
                    using (var flagImg = Paloma.TargaImage.LoadTargaImage(Path.Combine(flagsPath, $"{flag}.tga")))
                    {
                        flagAtlasG.DrawImage(flagImg, (i % atlasSize) * flagSize, (i / atlasSize) * flagSize);
                    }

                    ++i;
                    atlasDict.Add(dict.ContainsKey(flag) ? dict[flag] : flag);
                }

                flagAtlas.Save("FlagAtlas.png", ImageFormat.Png);
                File.WriteAllText("FlagAtlasDict.json", JsonConvert.SerializeObject(atlasDict));
            }

            Console.WriteLine("Done!");

            exit();
        }

        private class DictComparer : IComparer<string>
        {
            private readonly Dictionary<string, string> dict;

            public DictComparer(Dictionary<string, string> dict)
            {
                this.dict = dict;
            }

            public int Compare(string left, string right)
            {
                if (dict.ContainsKey(left) && dict.ContainsKey(right))
                    return dict[left].CompareTo(dict[right]);

                if (dict.ContainsKey(left) && !dict.ContainsKey(right))
                    return -1;

                if (!dict.ContainsKey(left) && dict.ContainsKey(right))
                    return 1;

                return left.CompareTo(right);
            }
        }
    }
}