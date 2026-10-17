using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The three art directions the Hell effects are tuned for (docs/HELL-ART-PASS.md section 4).</summary>
    public enum HellFxLook { InkedInferno, MoltenGrit, BlackAndEmber }

    /// <summary>
    /// Every colour and the main strengths of the Hell signature effects in one place (docs/HELL-FX.md). Static fields, so the
    /// look lane's art-direction switch (or a debug console) can change them at runtime: <see cref="Use"/> sets a whole
    /// direction. Glow colours are HDR (above 1 blooms). The effects read these every time they spawn something, so a change
    /// shows on the next effect. Hostile lime is deliberately absent: it belongs to the enemy outlines.
    /// </summary>
    public static class HellFxPalette
    {
        // ------------------------------------------------------------------ Shadow (Shadow Step, Nightfall, the Shade)
        /// <summary>The ink: the near-black violet of shadow smoke, the smear and the arrival splash.</summary>
        public static Color Ink = new Color(0.06f, 0.035f, 0.09f);
        /// <summary>The Shadow hue (#9B6BFF) for the marker, the tendril's core and the arrival ring.</summary>
        public static Color Shadow = new Color32(0x9B, 0x6B, 0xFF, 255);
        public static float ShadowGlow = 2.2f;

        // ------------------------------------------------------------------ Time (the Rewind, Time Dilation)
        /// <summary>The Rewind's hue: a cold cyan-teal (the HUD's Rewind bar), the ghost paths and the snap.</summary>
        public static Color Rewind = new Color(0.35f, 0.95f, 1f);
        public static float RewindGlow = 2.4f;
        /// <summary>Time Dilation's deep blue-violet (screen edge, grade tint, the bolts' afterimages).</summary>
        public static Color Dilation = new Color(0.36f, 0.3f, 1f);
        public static float DilationGlow = 2f;

        // ------------------------------------------------------------------ Fire (kills, Combustion, lava)
        public static Color Fire = new Color32(0xFF, 0x7A, 0x1F, 255);
        /// <summary>Embers: a hot yellow-orange (HDR).</summary>
        public static Color Ember = new Color(3.2f, 1.5f, 0.35f);
        /// <summary>The lava's surface glow pulses (HDR).</summary>
        public static Color LavaGlow = new Color(3f, 1.05f, 0.25f);
        /// <summary>The collapse's dust and grit (lit).</summary>
        public static Color Dust = new Color(0.32f, 0.24f, 0.2f);
        /// <summary>A crack opening in a collapsing platform (lit, near black with a hot edge from <see cref="Ember"/>).</summary>
        public static Color Crack = new Color(0.05f, 0.02f, 0.02f);

        // ------------------------------------------------------------------ strengths (1 = as designed)
        /// <summary>Scales every screen treatment (grade, chroma, lens, overlays).</summary>
        public static float ScreenStrength = 1f;
        /// <summary>Scales the counts of world effects (motes, embers, segments): lower for a slow machine.</summary>
        public static float Density = 1f;

        /// <summary>Sets a whole direction. A: the ink defaults (opaque blobs, strong grade). B: warmer, less ink, softer grade.
        /// C: colour-free stone, so the effects carry the colour: brighter glows, stronger grade.</summary>
        public static void Use(HellFxLook look)
        {
            Reset();
            switch (look)
            {
                case HellFxLook.MoltenGrit:
                    Ink = new Color(0.09f, 0.05f, 0.06f);
                    ShadowGlow = 2.6f;
                    Dust = new Color(0.38f, 0.3f, 0.26f);
                    ScreenStrength = 0.85f;
                    break;
                case HellFxLook.BlackAndEmber:
                    Ink = new Color(0.02f, 0.015f, 0.03f);
                    ShadowGlow = 3f;
                    RewindGlow = 3f;
                    DilationGlow = 2.6f;
                    Ember = new Color(4f, 1.7f, 0.35f);
                    Dust = new Color(0.22f, 0.21f, 0.21f);
                    ScreenStrength = 1.1f;
                    break;
            }
            Current = look;
        }

        public static HellFxLook Current { get; private set; } = HellFxLook.InkedInferno;

        public static void Reset()
        {
            Ink = new Color(0.06f, 0.035f, 0.09f);
            Shadow = new Color32(0x9B, 0x6B, 0xFF, 255);
            ShadowGlow = 2.2f;
            Rewind = new Color(0.35f, 0.95f, 1f);
            RewindGlow = 2.4f;
            Dilation = new Color(0.36f, 0.3f, 1f);
            DilationGlow = 2f;
            Fire = new Color32(0xFF, 0x7A, 0x1F, 255);
            Ember = new Color(3.2f, 1.5f, 0.35f);
            LavaGlow = new Color(3f, 1.05f, 0.25f);
            Dust = new Color(0.32f, 0.24f, 0.2f);
            Crack = new Color(0.05f, 0.02f, 0.02f);
            ScreenStrength = 1f;
            Density = 1f;
            Current = HellFxLook.InkedInferno;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();

        /// <summary>A glow colour at its strength (HDR).</summary>
        public static Color Hdr(Color c, float glow) => new Color(c.r * glow, c.g * glow, c.b * glow, 1f);
    }
}
