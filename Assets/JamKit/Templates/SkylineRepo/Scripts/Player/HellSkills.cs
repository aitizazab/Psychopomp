using System;
using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The floor skills (docs/HELL-ABILITIES.md, "How to add a skill"): each is one <see cref="HellSkillDef"/> entry in
    /// <see cref="All"/> plus one <see cref="HellSkill"/> component, paired in <see cref="Components"/>. The pick screen
    /// (<see cref="HellSkillPick"/>) offers three not yet owned; <see cref="HellAbilities.Grant"/> adds the component.
    /// </summary>
    public static class HellSkills
    {
        public const string UmbralChain = "umbral_chain", Ambush = "ambush", Shade = "shade", Nightfall = "nightfall",
            DeeperRewind = "deeper_rewind", MomentumBank = "momentum_bank", BorrowedTime = "borrowed_time",
            Ashwalker = "ashwalker", Combustion = "combustion";

        public static readonly HellSkillDef[] All =
        {
            new HellSkillDef { id = UmbralChain, name = "Umbral Chain", flavour = HellFlavour.Shadow, icon = "»»",
                description = "After a Shadow Step, step again for free within 1 second." },
            new HellSkillDef { id = Ambush, name = "Ambush", flavour = HellFlavour.Shadow, icon = "x3",
                description = "Your first shot after a Shadow Step does triple damage. +1 Shadow Step charge." },
            new HellSkillDef { id = Shade, name = "Shade", flavour = HellFlavour.Shadow, icon = "@",
                description = "A Shadow Step leaves your shadow behind. Enemies go after it for 4 seconds." },
            new HellSkillDef { id = Nightfall, name = "Nightfall", flavour = HellFlavour.Shadow, icon = "( )",
                description = "Floor skill button: a sphere of darkness for 8 seconds where you aim. Enemies inside lose you, and you can Shadow Step into it." },
            new HellSkillDef { id = DeeperRewind, name = "Deeper Rewind", flavour = HellFlavour.Time, icon = "6s",
                description = "Rewind goes back 6 seconds instead of 4." },
            new HellSkillDef { id = MomentumBank, name = "Momentum Bank", flavour = HellFlavour.Time, icon = ">>",
                description = "Time Dilation lasts longer the faster you move when you aim: up to 6 seconds." },
            new HellSkillDef { id = BorrowedTime, name = "Borrowed Time", flavour = HellFlavour.Time, icon = "-s",
                description = "Every kill takes 1.5 s off Shadow Step and 3 s off Rewind." },
            new HellSkillDef { id = Ashwalker, name = "Ashwalker", flavour = HellFlavour.Fire, icon = "~^~",
                description = "Shadow Step onto lava and walk it unharmed for 6 seconds. Jump as you touch lava to bounce off it." },
            new HellSkillDef { id = Combustion, name = "Combustion", flavour = HellFlavour.Fire, icon = "*!*",
                description = "Enemies that die burning explode, hurting and burning enemies near them." },
        };

        /// <summary>Each skill's component.</summary>
        public static readonly Dictionary<string, Type> Components = new Dictionary<string, Type>
        {
            { UmbralChain, typeof(HellSkillPassive) }, { MomentumBank, typeof(HellSkillPassive) }, { Ashwalker, typeof(HellSkillPassive) },
            { Ambush, typeof(HellSkillAmbush) }, { Shade, typeof(HellSkillShade) }, { Nightfall, typeof(HellSkillNightfall) },
            { DeeperRewind, typeof(HellSkillDeeperRewind) }, { BorrowedTime, typeof(HellSkillBorrowedTime) },
            { Combustion, typeof(HellSkillCombustion) },
        };

        public static HellSkillDef Find(string id)
        {
            foreach (var s in All) if (s.id == id) return s;
            return null;
        }

        public static Color FlavourColour(HellFlavour f) =>
            f == HellFlavour.Shadow ? (Color)new Color32(0x9B, 0x6B, 0xFF, 255) :
            f == HellFlavour.Time ? (Color)new Color32(0x5F, 0xE3, 0xF0, 255) : (Color)new Color32(0xFF, 0x7A, 0x1F, 255);

        internal static void AddComponent(HellAbilities a, string id)
        {
            if (!Components.TryGetValue(id, out var type) || type == null) return;
            var c = (HellSkill)a.gameObject.AddComponent(type);
            c.Init(a, id);
        }
    }

    /// <summary>A floor skill's component, on the player. <see cref="OnGranted"/> runs once when it is picked; the skill then
    /// works through the hooks (<see cref="HellAbilities.Stepped"/>, <see cref="HellRewind.Rewound"/>, the player's
    /// <c>CueRaised</c> / <c>HitLanded</c> / <c>AttackTaken</c> / <c>ShotDamageScale</c>, <see cref="SkylineBot.Killed"/>,
    /// <see cref="HellBurn.KilledWhileBurning"/>, <see cref="HellTime"/>, <see cref="HellDarkness"/>).</summary>
    public abstract class HellSkill : MonoBehaviour
    {
        public HellAbilities Abilities { get; private set; }
        public string Id { get; private set; }

        internal void Init(HellAbilities a, string id)
        {
            Abilities = a;
            Id = id;
            OnGranted();
        }

        protected virtual void OnGranted() { }
    }

    /// <summary>Skills the core reads as a flag (<see cref="HellAbilities.Has"/>): Umbral Chain, Momentum Bank, Ashwalker.</summary>
    public sealed class HellSkillPassive : HellSkill { }

    /// <summary>Ambush: +1 Shadow Step charge; the first shot after a step does triple damage.</summary>
    public sealed class HellSkillAmbush : HellSkill
    {
        public const float Multiplier = 3f;
        bool _armed;

        protected override void OnGranted()
        {
            Abilities.StepCharges.SetMax(Abilities.StepCharges.Max + 1);
            Abilities.Stepped += (a, b) => _armed = true;
            Abilities.Player.ShotDamageScale = Scale;
        }

        float Scale()
        {
            // the first shot after a step, or the first shot from hiding (docs/HELL-SHADOW.md)
            bool fromShadow = Abilities.Targeting != null && Abilities.Targeting.Stealth.TakeShotFromShadow();
            if (!_armed && !fromShadow) return 1f;
            _armed = false;
            Debug.Log("HELL_SKILL ambush shot x" + Multiplier);
            return Multiplier;
        }
    }

    /// <summary>Shade: a step leaves the player's shadow at the start; enemies target it for <see cref="Seconds"/>.</summary>
    public sealed class HellSkillShade : HellSkill
    {
        public const float Seconds = 4f;
        protected override void OnGranted() =>
            Abilities.Stepped += (from, to) => Abilities.Targeting.LeaveShade(from, Abilities.Player.Yaw, Seconds);
    }

    /// <summary>Nightfall: the floor skill button throws a sphere of darkness where you aim (within 30 m): radius 6 m, 8 s, cooldown 22 s.</summary>
    public sealed class HellSkillNightfall : HellSkill
    {
        public float radius = 6f, seconds = 8f, range = 30f;
        public AbilityCooldown Cooldown { get; } = new AbilityCooldown(22f);

        void Update()
        {
            Cooldown.Tick(Time.deltaTime);
            if (!HellAbilities.SkillPressed() || !HellAbilityWheel.Binding.Fires(HellSkills.Nightfall)) return;   // the wheel binds the Skill button
            if (!Cooldown.TryUse()) { HellAbilityAudio.Play("nightfall_denied", transform.position); return; }
            var p = Abilities.Player;
            Vector3 eye = p.EyePosition, aim = p.AimDirection;
            Vector3 c = Physics.Raycast(eye, aim, out var hit, range, ~(1 << 2), QueryTriggerInteraction.Ignore) ? hit.point : eye + aim * range;
            HellDarkness.AddSphere(c, radius, seconds);
            HellFx.NightfallSphere(c, radius, seconds);
            HellAbilityAudio.Play("nightfall", c);
            Debug.Log("HELL_SKILL nightfall at " + c.ToString("F1"));
        }
    }

    /// <summary>Deeper Rewind: 6 s instead of 4.</summary>
    public sealed class HellSkillDeeperRewind : HellSkill
    {
        protected override void OnGranted() => Abilities.Rewind.seconds = HellRewind.MaxSeconds;
    }

    /// <summary>Borrowed Time: each kill shaves the Shadow Step recharge and the Rewind cooldown.</summary>
    public sealed class HellSkillBorrowedTime : HellSkill
    {
        public float stepSeconds = 1.5f, rewindSeconds = 3f;

        void OnEnable() => SkylineBot.Killed += OnKill;
        void OnDisable() => SkylineBot.Killed -= OnKill;

        void OnKill(SkylineBot bot, GunDef gun)
        {
            if (Abilities == null) return;
            Abilities.StepCharges.Shave(stepSeconds);
            var cd = Abilities.Rewind.Cooldown;
            cd.Set(cd.Remaining - rewindSeconds);
        }
    }

    /// <summary>Combustion: an enemy that dies burning explodes (radius <see cref="Radius"/>), hurting the enemies near it and
    /// setting them burning, so blasts can chain.</summary>
    public sealed class HellSkillCombustion : HellSkill
    {
        public const float Radius = 5f, Damage = 45f, BurnSeconds = 3f, BurnDps = 10f, ChainDelay = 0.12f;
        readonly List<(Vector3 at, float when)> _queue = new List<(Vector3, float)>();

        void OnEnable() => HellBurn.KilledWhileBurning += Queue;
        void OnDisable() => HellBurn.KilledWhileBurning -= Queue;

        // a beat between blasts, so a chain reads as one after another (and no blast runs inside another's damage)
        void Queue(SkylineBot dead, Vector3 at) => _queue.Add((at, Time.time + ChainDelay));

        void Update()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if (Time.time < _queue[i].when) continue;
                var at = _queue[i].at;
                _queue.RemoveAt(i--);
                Explode(at);
            }
        }

        void Explode(Vector3 at)
        {
            HellFx.FireBlast(at, Radius);
            HellAbilityAudio.Play("combustion", at);
            var near = new List<SkylineBot>();
            foreach (var b in SkylineBot.Live) if (b != null && b.Alive && (b.Center - at).sqrMagnitude <= Radius * Radius) near.Add(b);
            foreach (var b in near)
            {
                HellBurn.Apply(b, BurnSeconds, BurnDps);
                var dir = (b.Center - at).normalized;
                b.TakeHit(new SkylineHitInfo
                {
                    gun = null, damage = Damage, zone = HitZone.Body, plate = -1, point = b.Center, normal = -dir, direction = dir,
                    knockback = 0.6f, time = Time.time,
                });
            }
        }
    }
}
