using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The Hell enemies' colours (docs/HELL-ENEMIES.md): charred black-red bodies, bone-orange horns and armour, and
    /// HDR glows (cores, eyes, flames, embers, lava, beams) that bloom. Enemy clarity on dark rock and lava glow: dark bodies
    /// against the glow, the hottest white-yellow kept for the weak points, and every body in the hostile ink role.</summary>
    public static class HellLook
    {
        public static readonly Color Char = new Color(0.08f, 0.035f, 0.03f);
        public static readonly Color Flesh = new Color(0.36f, 0.06f, 0.04f);
        public static readonly Color Bone = new Color(0.86f, 0.5f, 0.2f);
        public static readonly Color Rust = new Color(0.46f, 0.13f, 0.05f);
        public static readonly Color Crust = new Color(0.12f, 0.085f, 0.075f);
        public static readonly Color Steel = new Color(0.22f, 0.2f, 0.2f);

        /// <summary>The weak points: white-yellow, the hottest thing on every body.</summary>
        public static readonly Color CoreHot = new Color(5f, 3.6f, 1.3f);
        public static readonly Color EyeHot = new Color(5f, 3f, 0.4f);
        public static readonly Color EmberHot = new Color(4.5f, 1.8f, 0.35f);
        public static readonly Color EmberTrail = new Color(3.2f, 0.7f, 0.1f);
        public static readonly Color Flame = new Color(3.4f, 1.0f, 0.15f);
        public static readonly Color FlameDeep = new Color(2.6f, 0.35f, 0.06f);
        public static readonly Color FlameYellow = new Color(4.2f, 2.6f, 0.6f);
        public static readonly Color Lava = new Color(2.2f, 0.5f, 0.07f);
        public static readonly Color LavaHot = new Color(3.6f, 1.4f, 0.25f);
        public static readonly Color LavaCold = new Color(0.1f, 0.05f, 0.04f);
        public static readonly Color BeamHot = new Color(5f, 1.5f, 0.35f);
        public static readonly Color TelegraphLine = new Color(2.6f, 0.3f, 0.08f);

        public static Material Lit(Color c) => SkylineBotLook.Lit(c);
        public static Material Glow(Color hdr) => SkylineBotLook.Glow(hdr);
        /// <summary>A glow in the hostile ink role: outlined like the enemies (scythes, beams, pools, bolts).</summary>
        public static Material HostileGlow(Color hdr) => SkylineBotLook.Glow(hdr, SkylineInkRoles.Hostile);
        public static Material HostileLit(Color c) => SkylineBotLook.Lit(c, SkylineInkRoles.Hostile);
    }
}
