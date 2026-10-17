using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>Which mix bus a Hell sound plays on (SkylineMix maps each to its category).</summary>
    public enum HellSoundBus
    {
        /// <summary>The player's own abilities: at the guns' level, never slowed or muffled by Time Dilation.</summary>
        Ability,
        /// <summary>The enemies, their attacks and their hazards: the bots' bus (-12 dB), slowed and muffled by Time Dilation.</summary>
        Enemy,
        /// <summary>The rooms: lava, platforms, objectives, the shaft (-6 dB), slowed and muffled by Time Dilation.</summary>
        World,
        /// <summary>Beds and loops (the ambience bus), slowed and muffled by Time Dilation.</summary>
        Bed,
        /// <summary>The player's screen moments (the skill pick, the death, room cleared): the feedback bus, never slowed.</summary>
        Ui,
    }

    /// <summary>One named cue: what plays it (<see cref="Cue"/>, the name the code uses) and the file it plays (<see cref="File"/>,
    /// under Resources/SkylineAudio, no extension; empty = silent on purpose).</summary>
    public sealed class HellSoundCue
    {
        public readonly string Cue, File, Note;
        public readonly HellSoundBus Bus;
        public readonly bool Loop;

        public HellSoundCue(string cue, string file, HellSoundBus bus, bool loop, string note)
        {
            Cue = cue; File = file; Bus = bus; Loop = loop; Note = note;
        }
    }

    /// <summary>
    /// THE SWAP TABLE (docs/HELL-SOUND-PASS.md, "Installed"): every Hell sound cue -> the file it plays. To change a sound, change
    /// its row's file (the files are built by tools/audio/make-hell-sounds.py; any WAV/OGG under Resources/SkylineAudio works). An
    /// empty file silences the cue (an enemy goes voiceless: the old robot sounds are borrowed only for telegraph, attack, plate
    /// shed and wreck). The creator's open answers each have a switch below the table.
    /// <para>Cue names: <c>hell/&lt;ability id&gt;</c> (HellAbilityAudio.Play), <c>hell/&lt;enemy&gt;/&lt;name&gt;</c> (SkylineBot.HellSound),
    /// and the rest raised by HellSoundBinder (<see cref="HellSoundCues"/>).</para>
    /// </summary>
    public static class HellSoundTable
    {
        const HellSoundBus Ab = HellSoundBus.Ability, En = HellSoundBus.Enemy, Wo = HellSoundBus.World, Be = HellSoundBus.Bed, Ui = HellSoundBus.Ui;

        // Defaults while the creator's answers are open (2026-10-18): Shadow Step ghostly (Dishonored), Rewind a deep swirl ending on
        // a tick (Tracer), Time Dilation muffles and slows the world with a heartbeat, the imp and the demon have voices, a bed per
        // floor, CC BY picks kept (credited).
        public static readonly HellSoundCue[] Rows =
        {
            // ---- abilities (HellAbilityAudio.Play ids, and the binder's)
            new HellSoundCue("hell/shadowstep_aim", "hell/abilities/shadowstep_aim", Ab, true, "while E is held (the landing preview)"),
            new HellSoundCue("hell/shadowstep_out", "hell/abilities/shadowstep_out", Ab, false, "ghostly: backwards whoosh + cloth snap"),
            new HellSoundCue("hell/shadowstep_out_free", "hell/abilities/shadowstep_out_free", Ab, false, "Umbral Chain's free step (layered on the out)"),
            new HellSoundCue("hell/shadowstep_in", "hell/abilities/shadowstep_in", Ab, false, "a puff + weight"),
            new HellSoundCue("hell/shadowstep_denied", "hell/abilities/denied", Ab, false, ""),
            new HellSoundCue("hell/rewind_denied", "hell/abilities/denied", Ab, false, ""),
            new HellSoundCue("hell/nightfall_denied", "hell/abilities/denied", Ab, false, ""),
            new HellSoundCue("hell/rewind", "hell/abilities/rewind", Ab, false, "deep swirl (Tracer-like): tape stop, vortex, sub"),
            new HellSoundCue("hell/rewind_end", "hell/abilities/rewind_end", Ab, false, "0.3 s after the rewind: the tick"),
            new HellSoundCue("hell/dilation_in", "hell/abilities/dilation_in", Ab, false, ""),
            new HellSoundCue("hell/dilation_heartbeat", "hell/abilities/dilation_heartbeat", Ab, true, "while time is slowed (DilationHeartbeat)"),
            new HellSoundCue("hell/dilation_out", "hell/abilities/dilation_out", Ab, false, "CC BY"),
            new HellSoundCue("hell/ambush", "hell/abilities/ambush", Ab, false, "the tripled shot"),
            new HellSoundCue("hell/shade", "hell/abilities/shade", Ab, false, ""),
            new HellSoundCue("hell/nightfall", "hell/abilities/nightfall", Ab, false, "layered: inrush, dark rise, sub boom"),
            new HellSoundCue("hell/borrowed_time", "hell/abilities/borrowed_time", Ab, false, "a tick on each kill"),
            new HellSoundCue("hell/lava_hop", "hell/abilities/lava_hop", Ab, false, ""),
            new HellSoundCue("hell/lava_walk", "hell/abilities/lava_walk", Ab, false, "Ashwalker's steps on lava"),
            new HellSoundCue("hell/combustion", "hell/abilities/combustion", Ab, false, ""),
            new HellSoundCue("hell/skill_pick", "hell/ui/skill_pick", Ui, false, "the confirm"),
            new HellSoundCue("hell/skill_open", "hell/ui/skill_open", Ui, false, ""),
            new HellSoundCue("hell/skill_hover", "hell/ui/skill_hover", Ui, false, ""),
            new HellSoundCue("hell/death", "hell/ui/death", Ui, false, "the AGAIN banner"),
            new HellSoundCue("hell/room_cleared", "hell/ui/room_cleared", Ui, false, "one bell toll"),

            // ---- the fire imp (SkylineBot.HellSound names, plus the binder's)
            new HellSoundCue("hell/fire-imp/hover_loop", "hell/fire-imp/hover_loop", En, true, "wings + crackle"),
            new HellSoundCue("hell/fire-imp/telegraph", "hell/fire-imp/telegraph", En, false, ""),
            new HellSoundCue("hell/fire-imp/attack", "hell/fire-imp/attack", En, false, ""),
            new HellSoundCue("hell/fire-imp/bolt_hit", "hell/fire-imp/bolt_hit", En, false, ""),
            new HellSoundCue("hell/fire-imp/alert", "hell/fire-imp/alert", En, false, "voice (empty = voiceless)"),
            new HellSoundCue("hell/fire-imp/hurt", "hell/fire-imp/hurt", En, false, "voice"),
            new HellSoundCue("hell/fire-imp/wreck", "hell/fire-imp/wreck", En, false, "shriek cut by a fireball"),
            new HellSoundCue("hell/fire-imp/plate_shed", "hell/fire-imp/plate_shed", En, false, ""),

            // ---- the scythe demon and its scythes
            new HellSoundCue("hell/scythe-demon/alert", "hell/scythe-demon/alert", En, false, "voice"),
            new HellSoundCue("hell/scythe-demon/hurt", "hell/scythe-demon/hurt", En, false, "voice"),
            new HellSoundCue("hell/scythe-demon/telegraph", "hell/scythe-demon/telegraph", En, false, ""),
            new HellSoundCue("hell/scythe-demon/attack", "hell/scythe-demon/attack", En, false, "the volley appears: rift + shing + whir"),
            new HellSoundCue("hell/scythe-demon/footstep", "hell/scythe-demon/footstep", En, false, "one per leg plant"),
            new HellSoundCue("hell/scythe-demon/plate_shed", "hell/scythe-demon/plate_shed", En, false, ""),
            new HellSoundCue("hell/scythe-demon/wreck", "hell/scythe-demon/wreck", En, false, ""),
            new HellSoundCue("hell/scythe/warn", "hell/scythe/warn", En, false, "synthesised, 0.9 s, ends exactly on the beam"),
            new HellSoundCue("hell/scythe/beam", "hell/scythe/beam", En, false, ""),

            // ---- the lava ooze and its pools
            new HellSoundCue("hell/lava-ooze/throb", "hell/lava-ooze/throb", En, false, "one blorp per OozePulse beat"),
            new HellSoundCue("hell/lava-ooze/alert", "hell/lava-ooze/alert", En, false, ""),
            new HellSoundCue("hell/lava-ooze/telegraph", "hell/lava-ooze/telegraph", En, false, ""),
            new HellSoundCue("hell/lava-ooze/attack", "hell/lava-ooze/attack", En, false, ""),
            new HellSoundCue("hell/lava-ooze/hurt", "hell/lava-ooze/hurt", En, false, ""),
            new HellSoundCue("hell/lava-ooze/wreck", "hell/lava-ooze/wreck", En, false, "CC BY"),
            new HellSoundCue("hell/lava-ooze/plate_shed", "hell/lava-ooze/plate_shed", En, false, ""),
            new HellSoundCue("hell/lava-ooze/pool_loop", "hell/lava-ooze/pool_loop", En, true, "while a pool burns"),
            new HellSoundCue("hell/lava-ooze/pool_cool", "hell/lava-ooze/pool_cool", En, false, ""),
            new HellSoundCue("hell/spawn/warning", "hell/spawn/warning", En, false, "NOT WIRED: for the enemy-readability lane"),

            // ---- the rooms
            new HellSoundCue("hell/world/lava_bubble", "hell/world/lava_bubble_1", Wo, false, "variants _1.._3 picked at random"),
            new HellSoundCue("hell/world/lava_touch", "hell/world/lava_touch", Wo, false, "repeats while burning"),
            new HellSoundCue("hell/world/sink", "hell/world/sink", Wo, false, ""),
            new HellSoundCue("hell/world/platform_shake", "hell/world/platform_shake", Wo, false, ""),
            new HellSoundCue("hell/world/platform_fall", "hell/world/platform_fall", Wo, false, ""),
            new HellSoundCue("hell/world/fire_loop", "hell/world/fire_loop", Wo, true, "the nearest braziers"),
            new HellSoundCue("hell/world/shaft_start", "hell/world/shaft_start", Wo, false, ""),
            new HellSoundCue("hell/world/shaft_wind", "hell/world/shaft_wind", Wo, true, "rises through the fall"),
            new HellSoundCue("hell/world/shaft_land", "hell/world/shaft_land", Wo, false, ""),
            new HellSoundCue("hell/world/sigil", "hell/world/sigil", Wo, false, ""),
            new HellSoundCue("hell/world/lantern_pickup", "hell/world/lantern_pickup", Wo, false, ""),
            new HellSoundCue("hell/world/lantern_carry", "hell/world/lantern_carry", Wo, true, ""),
            new HellSoundCue("hell/world/lantern_place", "hell/world/lantern_place", Wo, false, ""),
            new HellSoundCue("hell/world/anchor_hit", "hell/world/anchor_hit", Wo, false, ""),
            new HellSoundCue("hell/world/anchor_deflect", "hell/world/anchor_deflect", Wo, false, ""),
            new HellSoundCue("hell/world/anchor_break", "hell/world/anchor_break", Wo, false, ""),
            new HellSoundCue("hell/world/brand_charge", "hell/world/brand_charge", Wo, true, "pitch follows the meter"),
            new HellSoundCue("hell/world/brand_done", "hell/world/brand_done", Wo, false, "CC BY"),
            new HellSoundCue("hell/world/well_grind", "hell/world/well_grind", Wo, true, "while a well is being sealed"),
            new HellSoundCue("hell/world/well_sealed", "hell/world/well_sealed", Wo, false, ""),
            new HellSoundCue("hell/world/gate_open", "hell/world/gate_open", Wo, false, ""),

            // ---- the ending (HellEndingDirector, docs/HELL-ENDING.md; reused files until the ear check)
            new HellSoundCue("hell/ending/rumble", "hell/world/platform_shake", Wo, false, "Hell shaking while you decide: more often as it builds"),
            new HellSoundCue("hell/ending/hold", "hell/world/sigil", Ab, false, "you start to offer your hand (the hold ring)"),
            new HellSoundCue("hell/ending/spare", "hell/world/brand_done", Ab, false, "she takes your hand"),
            new HellSoundCue("hell/ending/kill", "hell/world/anchor_break", Ab, false, "the shot that ends her: her body breaks into smoke"),
            new HellSoundCue("hell/ending/soul", "hell/world/lantern_pickup", Wo, false, "a soul into your lantern (hers on a kill, your sister's on the way)"),
            new HellSoundCue("hell/ending/step", "hell/abilities/shadowstep_in", Wo, false, "Vesper Shadow Steps beside you in the escape"),
            new HellSoundCue("hell/ending/escape", "hell/world/shaft_start", Wo, false, "the floor gives way: the escape starts"),
            new HellSoundCue("hell/ending/out", "hell/ui/room_cleared", Ui, false, "out of the Collapse: the endings begin"),
            new HellSoundCue("hell/ending/card", "hell/ui/skill_open", Ui, false, "an ending card"),

            // ---- the beds, one per floor (OneBedForAll swaps to a shared one)
            new HellSoundCue("hell/bed/0", "hell/beds/ember_gate", Be, true, "The Ember Gate: ash wind over the lava"),
            new HellSoundCue("hell/bed/1", "hell/beds/molten_works", Be, true, "The Molten Works: forge drone, chains, lava"),
            new HellSoundCue("hell/bed/2", "hell/beds/black_glass", Be, true, "The Black Glass: a cold drone"),

            // ---- music: none (the creator composes it with us). The hook: a row "hell/music/<floor>" with Bus Bed, played by the binder.
        };

        /// <summary>The variants of a cue picked at random (cue -> files).</summary>
        public static readonly Dictionary<string, string[]> Variants = new Dictionary<string, string[]>
        {
            { "hell/world/lava_bubble", new[] { "hell/world/lava_bubble_1", "hell/world/lava_bubble_2", "hell/world/lava_bubble_3" } },
        };

        // ---------------------------------------------------------------- the creator's open answers (swap points)

        /// <summary>Q3: Time Dilation muffles and slows the world (false: only its whooshes play).</summary>
        public static bool DilationMuffle = true;
        /// <summary>Q3: the heartbeat under slowed time.</summary>
        public static bool DilationHeartbeat = true;
        /// <summary>Q6: one shared bed everywhere instead of one per floor (the Ember Gate's).</summary>
        public static bool OneBedForAll = false;

        static Dictionary<string, HellSoundCue> _byCue;

        static Dictionary<string, HellSoundCue> ByCue
        {
            get
            {
                if (_byCue == null)
                {
                    var d = new Dictionary<string, HellSoundCue>(StringComparer.Ordinal);
                    foreach (var r in Rows) d[r.Cue] = r;
                    _byCue = d;
                }
                return _byCue;
            }
        }

        /// <summary>The row for <paramref name="cue"/>, or null for a cue the table doesn't know.</summary>
        public static HellSoundCue Find(string cue) => cue != null && ByCue.TryGetValue(cue, out var r) ? r : null;

        /// <summary>The file <paramref name="cue"/> plays: the table's row ("" = silent on purpose), else the cue itself (a file named
        /// like the cue still plays, as before the table).</summary>
        public static string FileFor(string cue)
        {
            var r = Find(cue);
            if (r == null) return cue;
            if (OneBedForAll && r.Bus == HellSoundBus.Bed && cue.StartsWith("hell/bed/", StringComparison.Ordinal)) return Rows[RowIndex("hell/bed/0")].File;
            return r.File;
        }

        /// <summary>The bus of <paramref name="cue"/>: its row's, else by its name (an ability id is <c>hell/&lt;id&gt;</c>, one level deep).</summary>
        public static HellSoundBus BusOf(string cue)
        {
            var r = Find(cue);
            if (r != null) return r.Bus;
            if (cue == null) return HellSoundBus.Enemy;
            if (cue.StartsWith("hell/world/", StringComparison.Ordinal)) return HellSoundBus.World;
            if (cue.StartsWith("hell/bed/", StringComparison.Ordinal) || cue.StartsWith("hell/music/", StringComparison.Ordinal)) return HellSoundBus.Bed;
            if (cue.StartsWith("hell/", StringComparison.Ordinal) && cue.IndexOf('/', 5) < 0) return HellSoundBus.Ability;
            return HellSoundBus.Enemy;
        }

        /// <summary>True for a bus Time Dilation slows and muffles (everything but the player's own and the screen moments).</summary>
        public static bool Dilated(HellSoundBus bus) => bus == HellSoundBus.Enemy || bus == HellSoundBus.World || bus == HellSoundBus.Bed;

        static int RowIndex(string cue)
        {
            for (int i = 0; i < Rows.Length; i++) if (Rows[i].Cue == cue) return i;
            return -1;
        }

        /// <summary>Every file the table can play (rows and variants), each once.</summary>
        public static List<string> AllFiles()
        {
            var list = new List<string>();
            foreach (var r in Rows) if (!string.IsNullOrEmpty(r.File) && !list.Contains(r.File)) list.Add(r.File);
            foreach (var kv in Variants) foreach (var f in kv.Value) if (!list.Contains(f)) list.Add(f);
            return list;
        }
    }

    /// <summary>The cues HellSoundBinder raises (the names in <see cref="HellSoundTable.Rows"/>).</summary>
    public static class HellSoundCues
    {
        public const string ShadowStepAim = "hell/shadowstep_aim", ShadowStepFree = "hell/shadowstep_out_free", RewindEnd = "hell/rewind_end",
            Heartbeat = "hell/dilation_heartbeat", Ambush = "hell/ambush", BorrowedTime = "hell/borrowed_time", LavaWalk = "hell/lava_walk",
            SkillOpen = "hell/skill_open", SkillHover = "hell/skill_hover", Death = "hell/death", RoomCleared = "hell/room_cleared",
            ImpHover = "hell/fire-imp/hover_loop", BoltHit = "hell/fire-imp/bolt_hit", DemonStep = "hell/scythe-demon/footstep",
            ScytheWarn = "hell/scythe/warn", ScytheBeam = "hell/scythe/beam", OozeThrob = "hell/lava-ooze/throb",
            PoolLoop = "hell/lava-ooze/pool_loop", PoolCool = "hell/lava-ooze/pool_cool", SpawnWarning = "hell/spawn/warning",
            LavaBubble = "hell/world/lava_bubble", LavaTouch = "hell/world/lava_touch", Sink = "hell/world/sink",
            PlatformShake = "hell/world/platform_shake", PlatformFall = "hell/world/platform_fall", FireLoop = "hell/world/fire_loop",
            ShaftStart = "hell/world/shaft_start", ShaftWind = "hell/world/shaft_wind", ShaftLand = "hell/world/shaft_land",
            Sigil = "hell/world/sigil", LanternPickup = "hell/world/lantern_pickup", LanternCarry = "hell/world/lantern_carry",
            LanternPlace = "hell/world/lantern_place", AnchorHit = "hell/world/anchor_hit", AnchorDeflect = "hell/world/anchor_deflect",
            AnchorBreak = "hell/world/anchor_break", BrandCharge = "hell/world/brand_charge", BrandDone = "hell/world/brand_done",
            WellGrind = "hell/world/well_grind", WellSealed = "hell/world/well_sealed", GateOpen = "hell/world/gate_open";

        /// <summary>The bed cue for a floor (0-based).</summary>
        public static string Bed(int floor) => "hell/bed/" + (floor < 0 ? 0 : floor > 2 ? 2 : floor);
    }
}
