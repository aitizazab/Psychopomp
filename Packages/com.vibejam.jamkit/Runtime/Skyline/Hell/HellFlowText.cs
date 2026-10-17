using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>One text card: a line on black and ember (the opening, the floor cards).</summary>
    public sealed class HellCard
    {
        /// <summary>Who speaks (shown small above the line), or null for narration.</summary>
        public string speaker;
        public string text;
        /// <summary>Seconds on screen before it moves on by itself (any confirm press moves on sooner).</summary>
        public float seconds;
        /// <summary>True: a placeholder line the creator rewrites (the creator writes all dialogue).</summary>
        public bool placeholder;

        public HellCard(string speaker, string text, float seconds, bool placeholder = true)
        {
            this.speaker = speaker; this.text = text; this.seconds = seconds; this.placeholder = placeholder;
        }
    }

    /// <summary>A floor's intro card.</summary>
    public sealed class HellFloorCard
    {
        public string number, name, tagline;
        public float seconds;
        /// <summary>The tagline is a placeholder (the floor names are the design's).</summary>
        public bool placeholder;
    }

    /// <summary>One line of the credits. <see cref="heading"/> lines are drawn larger, in ember.</summary>
    public sealed class HellCreditLine
    {
        public string text;
        public bool heading;
        /// <summary>A placeholder (to fill or confirm before submission).</summary>
        public bool placeholder;

        public HellCreditLine(string text, bool heading = false, bool placeholder = false)
        {
            this.text = text; this.heading = heading; this.placeholder = placeholder;
        }
    }

    /// <summary>
    /// Every word the flow shows (docs/HELL-FLOW.md): the title, the opening's cards, the floor cards, the credits. <b>All the
    /// story lines here are PLACEHOLDERS</b> (<c>placeholder: true</c>): the creator writes all dialogue and rewrites them.
    /// Change the words here only; the screens read them.
    /// </summary>
    public static class HellFlowText
    {
        /// <summary>The working name (the creator picks the title later).</summary>
        public const string Title = "HELL DESCENT";
        public const bool TitlePlaceholder = true;

        public const string GodOfDeath = "THE GOD OF DEATH";

        /// <summary>The God of Death's premise (HELL-DESIGN.md, "The creator's story pitch" and answers). PLACEHOLDER LINES.</summary>
        public static readonly HellCard[] Opening =
        {
            // PLACEHOLDER: the creator rewrites every line below.
            new HellCard(null, "Your sister is dead.", 3.2f),
            new HellCard(null, "You didn't grieve. You went, furious, to the one who took her.", 4.2f),
            new HellCard(GodOfDeath, "Mortals usually wait their turn. You're early.\nI could fix that.", 4.6f),
            new HellCard(GodOfDeath, "...Wait. I've had the funniest idea.", 3.6f),
            new HellCard(GodOfDeath, "Lucifer took a bride down there alive. Vesper.\nKill her. Teach him whose souls these are.\nAnd do me a few favours on the way.", 6.2f),
            new HellCard(GodOfDeath, "Shadow to hide in. Time to bend. A door into Hell, which I don't have.\nYour sister is down there too.\nOh, and don't take too long.", 6.6f),
        };

        public static readonly HellFloorCard[] Floors =
        {
            new HellFloorCard { number = "FLOOR ONE", name = "THE EMBER GATE", tagline = "Ash and cinders. Her first favours.", seconds = 2.8f, placeholder = true },
            new HellFloorCard { number = "FLOOR TWO", name = "THE MOLTEN WORKS", tagline = "Lava rivers. Chains. Things that cut.", seconds = 2.8f, placeholder = true },
            new HellFloorCard { number = "FLOOR THREE", name = "THE BLACK GLASS", tagline = "Obsidian and shadow. She is close.", seconds = 2.8f, placeholder = true },
        };

        public static HellFloorCard Floor(int floor) =>
            Floors[floor < 0 ? 0 : floor >= Floors.Length ? Floors.Length - 1 : floor];

        /// <summary>
        /// The credits, top to bottom. The CC BY lines come from docs/HELL-ART-PASS.md section 5: keep only the assets that
        /// ship in the build (CC BY needs the credit; CC0 doesn't, but we thank them). Placeholders are marked.
        /// </summary>
        public static readonly HellCreditLine[] Credits =
        {
            new HellCreditLine(Title, true, placeholder: TitlePlaceholder),
            new HellCreditLine("A descent in three floors"),
            new HellCreditLine(""),
            new HellCreditLine("MADE BY", true),
            new HellCreditLine("Muhammad Aitizaz Abro", placeholder: true),
            new HellCreditLine("[team and roles: to fill]", placeholder: true),
            new HellCreditLine(""),
            new HellCreditLine("STORY AND DIALOGUE", true),
            new HellCreditLine("[the creator]", placeholder: true),
            new HellCreditLine(""),
            new HellCreditLine("MODELS (CC BY 4.0, Sketchfab)", true),
            new HellCreditLine("\"Imp\" by Spinnee", placeholder: true),
            new HellCreditLine("\"FREE - Desolate Devil Scythe (GameReady)\" by nodgerty", placeholder: true),
            new HellCreditLine("\"Sacrificial Altar\" by Jochon", placeholder: true),
            new HellCreditLine("\"Hanging brazier\" by alexalbinyana", placeholder: true),
            new HellCreditLine(""),
            new HellCreditLine("MODELS (CC BY 3.0, Poly Pizza)", true),
            new HellCreditLine("\"Fire\" by Jakob Hippe", placeholder: true),
            new HellCreditLine("\"Pile of Bones\" by Zsky", placeholder: true),
            new HellCreditLine(""),
            new HellCreditLine("CC0 ART, WITH THANKS", true),
            new HellCreditLine("Quaternius   Kay Lousberg (KayKit)   Kenney", placeholder: true),
            new HellCreditLine("Poly Haven   ambientCG   iPoly3D   Pichuliru", placeholder: true),
            new HellCreditLine(""),
            new HellCreditLine("FONTS", true),
            new HellCreditLine("Anton, Bangers by Vernon Adams (SIL OFL 1.1)"),
            new HellCreditLine("Luckiest Guy by Astigmatic (Apache 2.0)"),
            new HellCreditLine(""),
            new HellCreditLine("SOUND AND MUSIC", true),
            new HellCreditLine("[from the sound pass: to fill]", placeholder: true),
            new HellCreditLine(""),
            new HellCreditLine("MADE WITH", true),
            new HellCreditLine("Unity"),
            new HellCreditLine(""),
            new HellCreditLine("THANK YOU FOR PLAYING", true),
        };

        // Small words on the screens (draft).
        public const string Start = "START", Chapters = "CHAPTERS", Settings = "SETTINGS", Quit = "QUIT", Continue = "CONTINUE", Back = "BACK";

        /// <summary>The chapter menu (HellFlow chapters 0-3).</summary>
        public static readonly string[] ChapterNames = { "FLOOR 1  THE EMBER GATE", "FLOOR 2  THE MOLTEN WORKS", "FLOOR 3  THE BLACK GLASS", "VESPER" };
        public const string SkipHint = "ENTER / A  next      ESC / B  skip";
        public const string MenuHint = "ENTER / A  select      ↑ ↓ / D-PAD  move";
        public const string StatsTitle = "THE DESCENT";

        /// <summary>The placeholder story lines (for the docs and a test that keeps them marked).</summary>
        public static IEnumerable<HellCard> PlaceholderCards()
        {
            foreach (var c in Opening) if (c.placeholder) yield return c;
        }
    }
}
