using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>One recording used in the Hell sounds (or the synthesised layers), with its credit line.</summary>
    public sealed class HellAudioSource
    {
        public readonly string Id, Title, Author, Url, Licence, Credit;

        public HellAudioSource(string id, string title, string author, string url, string licence, string credit)
        {
            Id = id; Title = title; Author = author; Url = url; Licence = licence; Credit = credit;
        }

        /// <summary>True for a licence that requires the credit line (CC BY), false for CC0 (credited as thanks).</summary>
        public bool CreditRequired => Licence != null && Licence.StartsWith("CC BY", StringComparison.Ordinal) && Id != "synth";
    }

    /// <summary>
    /// The Hell sounds' credits, for the credits screen and the repo's CREDITS.md. The data (<see cref="Sources"/>,
    /// <see cref="FileSources"/>) is generated with the files by tools/audio/make-hell-sounds.py (HellAudioCredits.Data.cs); the same
    /// lines are in Resources/HellAudioCredits.txt (licence TAB credit) for a screen that reads a text asset.
    /// </summary>
    public static partial class HellAudioCredits
    {
        /// <summary>The credit lines, CC BY first (required), then CC0 (thanks), then the synthesised layers.</summary>
        public static List<string> Lines()
        {
            var list = new List<string>();
            foreach (var s in Sources) if (s.CreditRequired) list.Add(s.Credit);
            foreach (var s in Sources) if (!s.CreditRequired && s.Id != "synth") list.Add(s.Credit);
            foreach (var s in Sources) if (s.Id == "synth") list.Add(s.Credit);
            return list;
        }

        /// <summary>The source by id, or null.</summary>
        public static HellAudioSource Source(string id)
        {
            foreach (var s in Sources) if (s.Id == id) return s;
            return null;
        }

        /// <summary>The sources layered into a shipped file (path under Resources/SkylineAudio, no extension), or an empty list.</summary>
        public static List<HellAudioSource> SourcesFor(string file)
        {
            var list = new List<HellAudioSource>();
            foreach (var row in FileSources)
            {
                if (row.Length == 0 || row[0] != file) continue;
                for (int i = 1; i < row.Length; i++)
                {
                    var s = Source(row[i]);
                    if (s != null) list.Add(s);
                }
            }
            return list;
        }

        /// <summary>The files among <paramref name="files"/> with no credited source (should be none).</summary>
        public static List<string> Uncredited(IEnumerable<string> files)
        {
            var list = new List<string>();
            foreach (var f in files) if (SourcesFor(f).Count == 0) list.Add(f);
            return list;
        }
    }
}
