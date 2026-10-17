using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>
    /// The God of Death's errands (docs/HELL-JOBS.md): small jobs off the main line, in a side pocket of a room, that she gives
    /// her minion because he happens to be in the area. Each is one verb on an existing system plus at most one small new
    /// behaviour.
    /// </summary>
    public enum HellJobVerb
    {
        /// <summary>Kill the target before it gets away (along its flee path, or through a rift after a few seconds).</summary>
        Escape,
        /// <summary>Stand by a soul long enough to reap it (progress decays while you're out of reach).</summary>
        Reap,
        /// <summary>Pick a thing up and carry it out of the room through its drop.</summary>
        Retrieve,
        /// <summary>Destroy an object (warded: shots bite only from close) before its keeper's timer runs out.</summary>
        Destroy,
        /// <summary>Break every chain; a living smith re-forges a broken one on a timer.</summary>
        Free,
        /// <summary>Kill the target in one burst: damage that doesn't kill within the window is undone.</summary>
        Burst,
        /// <summary>Hold a spot for a while; enemies come while you hold it.</summary>
        Hold,
    }

    public enum HellJobStatus { Idle, Active, Alerted, Done, Failed }

    public enum HellJobRewardKind
    {
        /// <summary>A small stat boon (<see cref="HellJobBoons"/>), applied at once, stacking.</summary>
        Boon,
        /// <summary>A floor skill of the given flavour, granted at once (from the same pool as the floor's skill pick, so the pick
        /// never offers it again); a boon instead when every skill of that flavour is owned.</summary>
        Gift,
    }

    /// <summary>The boons a job can give. Each stacks up to its cap (<see cref="HellJobBoons.Cap"/>).</summary>
    public static class HellJobBoons
    {
        /// <summary>Plating and chassis back to full.</summary>
        public const string Mend = "mend";
        /// <summary>Shadow Step recharges 1 s sooner (not under 3 s).</summary>
        public const string QuickStep = "quick_step";
        /// <summary>Rewind's cooldown 3 s shorter (not under 11 s).</summary>
        public const string ShortMemory = "short_memory";
        /// <summary>Time Dilation holds 0.75 s longer.</summary>
        public const string LongBreath = "long_breath";

        public const float QuickStepSeconds = 1f, QuickStepFloor = 3f, ShortMemorySeconds = 3f, ShortMemoryFloor = 11f, LongBreathSeconds = 0.75f;

        public static readonly string[] All = { Mend, QuickStep, ShortMemory, LongBreath };

        /// <summary>The boon given when a gift's flavour has nothing left (a time boon for time, and so on).</summary>
        public static string FallbackFor(HellFlavour f)
        {
            switch (f)
            {
                case HellFlavour.Shadow: return QuickStep;
                case HellFlavour.Time: return ShortMemory;
                default: return Mend;
            }
        }

        public static string Describe(string boon)
        {
            switch (boon)
            {
                case Mend: return "Mended: plating and chassis full";
                case QuickStep: return "Quick Shadow: Shadow Step recharges 1 s sooner";
                case ShortMemory: return "Short Memory: Rewind cools down 3 s sooner";
                case LongBreath: return "Long Breath: Time Dilation holds 0.75 s longer";
                default: return boon;
            }
        }

        /// <summary>A new Shadow Step recharge after one more Quick Shadow.</summary>
        public static float StepRecharge(float current) => Math.Max(QuickStepFloor, current - QuickStepSeconds);

        /// <summary>A new Rewind cooldown after one more Short Memory.</summary>
        public static float RewindCooldown(float current) => Math.Max(ShortMemoryFloor, current - ShortMemorySeconds);
    }

    /// <summary>One job as data: who, why (her briefing), the verb and its numbers, where it sits, and what it pays.</summary>
    public sealed class HellJobDef
    {
        public string id = "";
        /// <summary>The job's name on the card.</summary>
        public string name = "";
        /// <summary>The God of Death's one-line briefing (PLACEHOLDER: the creator rewrites every line).</summary>
        public string briefing = "";
        /// <summary>The short tracker line under the card (what to do).</summary>
        public string objective = "";
        /// <summary>Her line on success and on failure (PLACEHOLDER).</summary>
        public string doneLine = "", failLine = "";
        public HellJobVerb verb;
        public HellRoomKind room;
        /// <summary>0-based floor (0 the Ember Gate, 1 the Molten Works, 2 the Black Glass).</summary>
        public int floor;

        public HellJobRewardKind rewardKind;
        /// <summary>The boon's id (<see cref="HellJobBoons"/>) for a boon; ignored for a gift.</summary>
        public string boon = "";
        /// <summary>The gift's flavour.</summary>
        public HellFlavour giftFlavour;

        // ---- the verb's numbers (each verb reads its own)
        /// <summary>Escape / Burst / Destroy's keeper / Free's smith: the target's hit points (Destroy: the object's; Free: each chain's).</summary>
        public float health = 90f;
        /// <summary>Escape: seconds from the alert until the target is gone (its flee path is walked in this time).</summary>
        public float escapeSeconds = 7f;
        /// <summary>Escape: damage multiplier while the target hasn't noticed you (the Poacher: shots from the dark).</summary>
        public float sneakMultiplier = 1f;
        /// <summary>The target notices you within this distance (Unity also asks for sight, and that you're not in darkness when
        /// <see cref="darkHides"/>).</summary>
        public float alertRadius = 14f;
        public bool darkHides;
        /// <summary>Escape (the Poacher): the target looks round every <see cref="glanceEvery"/> s for <see cref="glanceSeconds"/>;
        /// it only notices you while looking round. 0: always looking.</summary>
        public float glanceEvery, glanceSeconds;
        /// <summary>Reap / Hold / Retrieve: the reach (horizontal metres from the feet).</summary>
        public float reach = 1.6f;
        /// <summary>Reap / Hold: seconds needed; Reap's decay per second out of reach.</summary>
        public float seconds = 2f, decay;
        /// <summary>Destroy / Free: shots bite only from within this distance (the eye to the object).</summary>
        public float wardRadius = 9f;
        /// <summary>Destroy: seconds from the alert until the keeper has filed it away (frozen while the keeper is dead).</summary>
        public float timerSeconds = 9f;
        /// <summary>Free: chains; seconds between re-forgings while the smith lives.</summary>
        public int chains = 3;
        public float reforgeSeconds = 5f;
        /// <summary>Burst: the window after the first hit; seconds between the target's hops.</summary>
        public float burstWindow = 1.2f, hopSeconds = 2.5f;
        /// <summary>Hold: seconds between the enemies that come; the most at once.</summary>
        public float spawnEvery = 3f;
        public int spawnCap = 3;
        /// <summary>A second, killable keeper (Destroy's clerk, Free's smith): its hit points; 0 = none.</summary>
        public float keeperHealth;

        public string RewardText(string grantedSkillName)
        {
            if (rewardKind == HellJobRewardKind.Gift)
                return grantedSkillName != null ? "Her gift: " + grantedSkillName : "Her gift: a " + giftFlavour.ToString().ToLowerInvariant() + " power";
            return HellJobBoons.Describe(boon);
        }
    }

    /// <summary>
    /// The nine jobs, three per floor, each in a different room kind (a run draws 4 of a floor's 6 rooms, so it always offers at
    /// least one of the floor's three and usually two: expected 2 per floor, 6 per run). All dialogue is PLACEHOLDER, marked for
    /// the creator to rewrite.
    /// </summary>
    public static class HellJobCatalog
    {
        public const string Viscount = "viscount", OverdueSoul = "overdue_soul", Hourglass = "hourglass", Ledger = "ledger",
            Poacher = "poacher", Whelp = "whelp", SecondsThief = "seconds_thief", Bather = "bather", Notice = "notice";

        public static readonly HellJobDef[] All =
        {
            // ---------------------------------------------------------------- floor 1, the Ember Gate
            new HellJobDef
            {
                id = Viscount, name = "The Viscount of Cinders", verb = HellJobVerb.Escape, room = HellRoomKind.SixSigils, floor = 0,
                briefing = "There's a viscount on a balcony down there who keeps sending me fruit baskets. Make it stop. Permanently.",
                objective = "Kill the Viscount before his palanquin reaches his door",
                doneLine = "No more pears. Bless you.", failLine = "He got away. Expect a basket. Addressed to YOU.",
                rewardKind = HellJobRewardKind.Gift, giftFlavour = HellFlavour.Shadow,
                health = 90f, escapeSeconds = 7f, alertRadius = 16f,
            },
            new HellJobDef
            {
                id = OverdueSoul, name = "The Overdue Soul", verb = HellJobVerb.Reap, room = HellRoomKind.BreakChains, floor = 0,
                briefing = "Albrecht Huss is three hundred years overdue. He's hiding in a cage. Cute. Step in and collect him.",
                objective = "Shadow Step into the cage and stand by Huss to reap him",
                doneLine = "Filed. Three centuries late, but filed.", failLine = "You walked past him. He waved. I saw.",
                rewardKind = HellJobRewardKind.Boon, boon = HellJobBoons.QuickStep,
                reach = 1.6f, seconds = 1.5f, decay = 1f,
            },
            new HellJobDef
            {
                id = Hourglass, name = "The Lost Hourglass", verb = HellJobVerb.Retrieve, room = HellRoomKind.OozeWells, floor = 0,
                briefing = "One of my reapers dropped my good hourglass down there. Fetch it. Don't shake it.",
                objective = "Take the hourglass and carry it out through the room's exit",
                doneLine = "Oh, it's even still running. Good boy.", failLine = "You left it. On the floor. In HELL.",
                rewardKind = HellJobRewardKind.Boon, boon = HellJobBoons.ShortMemory,
                reach = 1.8f,
            },
            // ---------------------------------------------------------------- floor 2, the Molten Works
            new HellJobDef
            {
                id = Ledger, name = "The Ledger of Ash", verb = HellJobVerb.Destroy, room = HellRoomKind.SoulLantern, floor = 1,
                briefing = "Some clerk is stamping my dead 'CANCELLED'. Burn his book. Not him. Well. Him too, if he fusses.",
                objective = "Burn the ledger before the clerk files it away (shots bite up close)",
                doneLine = "Ashes. Lovely. Paperwork is the real Hell.", failLine = "Filed. In triplicate. I hate him so much.",
                rewardKind = HellJobRewardKind.Boon, boon = HellJobBoons.LongBreath,
                health = 60f, wardRadius = 9f, timerSeconds = 9f, keeperHealth = 60f,
            },
            new HellJobDef
            {
                id = Poacher, name = "The Poacher", verb = HellJobVerb.Escape, room = HellRoomKind.HoldBrand, floor = 1,
                briefing = "A scythe demon is poaching my souls and selling them by the jar. Kill him from the dark. He spooks.",
                objective = "Kill the Poacher unseen: from the dark, before he opens a rift",
                doneLine = "Quietly done. I didn't even hear it, and I hear everything.", failLine = "He spooked. Through a rift, with my jars.",
                rewardKind = HellJobRewardKind.Gift, giftFlavour = HellFlavour.Time,
                health = 150f, escapeSeconds = 3f, sneakMultiplier = 3f, alertRadius = 18f, darkHides = true,
                glanceEvery = 4f, glanceSeconds = 1.2f,
            },
            new HellJobDef
            {
                id = Whelp, name = "The Whelp", verb = HellJobVerb.Free, room = HellRoomKind.Collapse, floor = 1,
                briefing = "Someone chained up my dog. My SMALL dog. Break the chains. The smith keeps re-forging them, so... deal with that.",
                objective = "Break all three chains (the smith re-forges one every 5 s)",
                doneLine = "Who's a good boy. Not you. HIM.", failLine = "You left my dog in chains. Noted.",
                rewardKind = HellJobRewardKind.Boon, boon = HellJobBoons.QuickStep,
                health = 40f, wardRadius = 9f, chains = 3, reforgeSeconds = 5f, keeperHealth = 60f,
            },
            // ---------------------------------------------------------------- floor 3, the Black Glass
            new HellJobDef
            {
                id = SecondsThief, name = "The Seconds Thief", verb = HellJobVerb.Burst, room = HellRoomKind.SixSigils, floor = 2,
                briefing = "A demon has been skimming seconds off my hourglass. He undoes any wound that doesn't finish him. So finish him.",
                objective = "Kill the Seconds Thief in one burst: he undoes any wound that doesn't kill",
                doneLine = "My seconds. Every last one.", failLine = "He's still out there, being ten seconds younger than me.",
                rewardKind = HellJobRewardKind.Boon, boon = HellJobBoons.ShortMemory,
                health = 80f, burstWindow = 1.2f, hopSeconds = 2.5f,
            },
            new HellJobDef
            {
                id = Bather, name = "The Lava Bather", verb = HellJobVerb.Reap, room = HellRoomKind.OozeWells, floor = 2,
                briefing = "A soul's been soaking in a lava spring for six hundred years and calls it a spa. Wade in. Reap him.",
                objective = "Get close to the Bather in the lava spring until he's reaped",
                doneLine = "Out of the bath. Finally. He was SO pruney.", failLine = "He's still in the tub. Smug.",
                rewardKind = HellJobRewardKind.Gift, giftFlavour = HellFlavour.Fire,
                reach = 2.5f, seconds = 2.5f, decay = 0.3f,
            },
            new HellJobDef
            {
                id = Notice, name = "The Notice on the Door", verb = HellJobVerb.Hold, room = HellRoomKind.SoulLantern, floor = 2,
                briefing = "A demon is renting out my old cottage down there. Stand at his door while my notice burns in. He'll send friends.",
                objective = "Hold the seal by the squatter's door until the notice burns in",
                doneLine = "Evicted. I may visit. I won't.", failLine = "The notice fell off. He's subletting now.",
                rewardKind = HellJobRewardKind.Boon, boon = HellJobBoons.Mend,
                reach = 2f, seconds = 6f, spawnEvery = 3f, spawnCap = 3,
            },
        };

        public static HellJobDef Find(string id)
        {
            foreach (var j in All) if (j.id == id) return j;
            return null;
        }

        /// <summary>The job in this room on this floor, or null.</summary>
        public static HellJobDef For(HellRoomKind kind, int floor)
        {
            foreach (var j in All) if (j.room == kind && j.floor == floor) return j;
            return null;
        }

        /// <summary>How many jobs a dealt deck offers (one per dealt room that has one).</summary>
        public static int Offered(HellRoomKind[][] deck)
        {
            int n = 0;
            if (deck == null) return 0;
            for (int f = 0; f < deck.Length; f++)
                foreach (var k in deck[f]) if (For(k, f) != null) n++;
            return n;
        }
    }

    /// <summary>
    /// The run's job tally: offered (entered a room with a job), done, failed. The ending reads <see cref="Favoured"/>: the God
    /// of Death spares you for sparing Vesper only if you did most of what she asked of you on the way (at least
    /// <see cref="FavourShare"/> of the jobs offered, and at least <see cref="FavourMinimum"/>).
    /// </summary>
    public sealed class HellJobLedger
    {
        public const float FavourShare = 2f / 3f;
        public const int FavourMinimum = 2;

        readonly List<string> _offered = new List<string>();
        readonly List<string> _done = new List<string>();
        readonly List<string> _failed = new List<string>();

        public int OfferedCount => _offered.Count;
        public int DoneCount => _done.Count;
        public int FailedCount => _failed.Count;
        public IReadOnlyList<string> Done => _done;
        public IReadOnlyList<string> Failed => _failed;
        public IReadOnlyList<string> OfferedJobs => _offered;

        public bool IsDone(string id) => _done.Contains(id);
        public bool IsSettled(string id) => _done.Contains(id) || _failed.Contains(id);

        public void Offer(string id) { if (!_offered.Contains(id)) _offered.Add(id); }

        /// <summary>Marks a job done (once). False when it was already settled.</summary>
        public bool MarkDone(string id)
        {
            if (IsSettled(id)) return false;
            Offer(id);
            _done.Add(id);
            return true;
        }

        public bool MarkFailed(string id)
        {
            if (IsSettled(id)) return false;
            Offer(id);
            _failed.Add(id);
            return true;
        }

        /// <summary>Jobs needed for her favour out of <paramref name="offered"/>.</summary>
        public static int Needed(int offered) => Math.Max(Math.Min(FavourMinimum, offered), (int)Math.Ceiling(offered * FavourShare - 1e-4));

        /// <summary>Did enough of the offered jobs to be spared for sparing Vesper.</summary>
        public bool Favoured => OfferedCount > 0 && DoneCount >= Needed(OfferedCount);

        public void Clear() { _offered.Clear(); _done.Clear(); _failed.Clear(); }
    }

    /// <summary>What a job's reward resolves to.</summary>
    public struct HellJobPayout
    {
        /// <summary>A skill id to grant (a gift), or null.</summary>
        public string skill;
        /// <summary>A boon id to apply, or null.</summary>
        public string boon;
    }

    public static class HellJobRewards
    {
        /// <summary>A gift is one skill of its flavour the player doesn't own (picked by <paramref name="nextInt"/>); with none left
        /// it becomes that flavour's boon. A boon is itself.</summary>
        public static HellJobPayout Resolve(HellJobDef job, IReadOnlyList<HellSkillDef> pool, ICollection<string> owned, Func<int, int> nextInt)
        {
            if (job.rewardKind == HellJobRewardKind.Boon) return new HellJobPayout { boon = job.boon };
            var ofFlavour = new List<HellSkillDef>();
            if (pool != null)
                foreach (var s in pool) if (s != null && s.flavour == job.giftFlavour) ofFlavour.Add(s);
            var offer = HellSkillPool.Offer(ofFlavour, owned, 1, nextInt);
            if (offer.Count > 0) return new HellJobPayout { skill = offer[0].id };
            return new HellJobPayout { boon = HellJobBoons.FallbackFor(job.giftFlavour) };
        }
    }
}
