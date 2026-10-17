using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>
    /// The story beats as DATA (docs/HELL-BEATS.md, "Swapping a set"). A set is a list of <see cref="HellBeatDef"/>; the game
    /// plays <see cref="Active"/> (the set named <see cref="ActiveName"/>; <c>-hell-beats &lt;name&gt;</c> on the command
    /// line picks another). To swap in the creator's pick: add a set here (copy <see cref="Placeholder"/>), put its words in
    /// <see cref="HellLines"/> under new keys, and set <see cref="ActiveName"/>. No code changes.
    /// <para><b>The "placeholder" set is a stand-in</b>: Vesper's three once-per-floor moments from docs/HELL-DESIGN.md
    /// (floor 1 her voice in the Soul Lantern, hostile; floor 2 her deal and her daughter, curious; floor 3 she sees herself
    /// in you). The creator didn't like them (2026-10-18); a writing agent is drafting option sets.</para>
    /// </summary>
    public static class HellBeatSets
    {
        public const string PlaceholderName = "placeholder";

        /// <summary>The set the game plays.</summary>
        public static string ActiveName = PlaceholderName;

        public static readonly Dictionary<string, Func<List<HellBeatDef>>> Sets =
            new Dictionary<string, Func<List<HellBeatDef>>>(StringComparer.OrdinalIgnoreCase)
            {
                { PlaceholderName, Placeholder },
            };

        /// <summary>The active set (the placeholder when <see cref="ActiveName"/> names none).</summary>
        public static List<HellBeatDef> Active => Sets.TryGetValue(ActiveName ?? "", out var make) ? make() : Placeholder();

        // ------------------------------------------------------------------ helpers for writing sets

        public static HellBeatWhen When(HellBeatTrigger t, int floor = -1, string kind = "", int room = -1) =>
            new HellBeatWhen(t) { floor = floor, kind = kind ?? "", room = room };

        public static HellBeatWhen After(float seconds, int floor, string kind) =>
            new HellBeatWhen(HellBeatTrigger.RoomTimer) { floor = floor, kind = kind, seconds = seconds };

        public static HellBeatWhen Progress(float atLeast, int floor, string kind) =>
            new HellBeatWhen(HellBeatTrigger.ObjectiveProgress) { floor = floor, kind = kind, atLeast = atLeast };

        // ------------------------------------------------------------------ the sets

        /// <summary>PLACEHOLDER: Vesper's three moments (each "when" list is in order of preference; later ones are fallbacks).</summary>
        public static List<HellBeatDef> Placeholder()
        {
            const string Lantern = nameof(HellRoomKind.SoulLantern), Sigils = nameof(HellRoomKind.SixSigils);
            return new List<HellBeatDef>
            {
                new HellBeatDef
                {
                    id = "vesper.f1.lantern",
                    lines = new[] { "beat.vesper.f1.lantern.1", "beat.vesper.f1.lantern.2" },
                    when =
                    {
                        When(HellBeatTrigger.LanternTaken, 0),                    // her voice in the lantern
                        After(25f, 0, Lantern),                                     // the lantern left on its pedestal
                        new HellBeatWhen(HellBeatTrigger.RoomEntered) { floor = 0, room = 3, unlessKindSeen = Lantern },   // never dealt
                        When(HellBeatTrigger.ShaftEntered, 0),                    // the last resort
                    },
                },
                new HellBeatDef
                {
                    id = "vesper.f2.deal",
                    lines = new[] { "beat.vesper.f2.deal.1", "beat.vesper.f2.deal.2" },
                    staging = HellBeatStaging.FigureAhead, figureDistance = 30f, figureSeconds = 6f,
                    when =
                    {
                        Progress(0.5f, 1, Sigils),                                  // halfway through the Sigils
                        When(HellBeatTrigger.ShaftEntered, 1),
                    },
                },
                new HellBeatDef
                {
                    id = "vesper.f3.mirror",
                    lines = new[] { "beat.vesper.f3.mirror.1", "beat.vesper.f3.mirror.2" },
                    letterbox = true, holdSeconds = 3.5f, holdScale = 0.15f,
                    when =
                    {
                        When(HellBeatTrigger.ShaftEntered, 2),                    // right before the boss
                        When(HellBeatTrigger.BeforeBoss),                         // a chapter start straight to Vesper
                        When(HellBeatTrigger.RunFinished),                        // a build without the boss
                    },
                },
            };
        }
    }
}
