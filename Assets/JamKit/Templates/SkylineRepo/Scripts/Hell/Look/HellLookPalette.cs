using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Every number of the Hell look in one place (docs/HELL-LIGHTING.md), per art direction and per floor: the surfaces' textures
    /// and tints, the lights (lava, braziers, the sky's glow), the ambient gradient, the fog and the sky beyond the rooms.
    /// Floors: 0 The Ember Gate (ash and cliff rock, orange fire), 1 The Molten Works (dark brick and iron plate, red foundry
    /// glow), 2 The Black Glass (black glassy rock, violet dark, embers). Light intensities are URP's (inverse-square: a light of
    /// intensity I lights a surface d metres away with about I / d^2).
    /// </summary>
    public sealed class HellFloorLook
    {
        // surfaces: texture set names in Resources/HellLook/Textures (<name>_diff.jpg, <name>_nor.jpg)
        public string wallTex, floorTex, deckTex, coverTex, trimTex;
        public Color wallTint, floorTint, deckTint, coverTint, trimTint;
        public float wallTile = 6f, floorTile = 4f, deckTile = 3f;
        public float floorSmooth = 0.2f, wallSmooth = 0.25f;
        public float posterize, desaturate, contrast = 1f, bands, normal = 1f;
        public Color veinColor;

        // light
        public Color lavaLight = new Color(1f, 0.42f, 0.12f);
        public float lavaIntensity = 16f, lavaRange = 14f, lavaHeight = 1.6f, lavaSpacing = 9f, lavaFlicker = 0.12f;
        public Color brazierLight = new Color(1f, 0.58f, 0.26f);
        public float brazierIntensity = 9f, brazierRange = 13f, brazierSpacing = 15f, brazierFlicker = 0.22f;
        public Color sun = new Color(1f, 0.4f, 0.25f);
        public float sunIntensity = 0.25f;
        public float poolStrength = 1.4f;   // the banded model's pool strength (A; enemies on InkLit in every direction)
        public Color ambientSky, ambientEquator, ambientGround;
        public Color haze = new Color(0.5f, 0.12f, 0.02f);
        public float hazeHeight = 4f;

        // fog
        public Color fog;
        public FogMode fogMode = FogMode.ExponentialSquared;
        public float fogDensity = 0.01f, fogStart = 30f, fogEnd = 220f;

        // the sky (Hell/Sky)
        public Color zenith, glow, smoke, smokeLit, ridgeFar, ridgeNear, rim, pillar;
        public float glowHeight = 0.12f, smokeAmount = 0.8f, ridgeHeight = 0.12f, spires = 0.6f, embers = 0.5f, seed;
        public Color spireTint;
        public string spireTex = "cliff";
        /// <summary>A dim ember the player carries (a warm light on the camera, Deep Rock Galactic's headlamp): 0 = none.</summary>
        public float carryLight;
        public Color spireVein;

        // post
        public float bloomIntensity = 1f, bloomThreshold = 1f, vignette = 0.3f, exposure;
        public bool aces, sceneryInk;
    }

    public static class HellLookPalette
    {
        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        public static HellFloorLook For(HellLookDirection d, int floor)
        {
            floor = Mathf.Clamp(floor, 0, 2);
            var L = Base(floor);
            switch (d)
            {
                case HellLookDirection.A: Inked(L, floor); break;
                case HellLookDirection.C: BlackAndEmber(L, floor); break;
                default: break;   // B (and None) = the base: photo-real grit
            }
            return L;
        }

        /// <summary>B Molten Grit: the photo textures under PBR light, warm fog, bloom. The base the other two adjust.</summary>
        static HellFloorLook Base(int f)
        {
            var L = new HellFloorLook();
            switch (f)
            {
                case 0: // The Ember Gate: stratified cliff walls, burned ash ground, volcanic tile walkways, bone-ash trims
                    L.wallTex = "cliff"; L.floorTex = "ash"; L.deckTex = "tiles"; L.coverTex = "basalt"; L.trimTex = "tiles";
                    L.wallTint = C(0.95f, 0.82f, 0.74f); L.floorTint = C(1.05f, 0.92f, 0.85f); L.deckTint = C(1.1f, 1.0f, 0.95f);
                    L.coverTint = C(1.1f, 0.95f, 0.9f); L.trimTint = C(0.75f, 0.62f, 0.5f);
                    L.veinColor = C(1.5f, 0.42f, 0.07f);
                    L.ambientSky = C(0.07f, 0.065f, 0.09f); L.ambientEquator = C(0.07f, 0.045f, 0.04f); L.ambientGround = C(0.24f, 0.09f, 0.03f);
                    L.sun = C(1f, 0.45f, 0.3f); L.sunIntensity = 0.3f;
                    L.fog = C(0.15f, 0.065f, 0.04f); L.fogDensity = 0.0085f;
                    L.zenith = C(0.02f, 0.012f, 0.01f); L.glow = C(1.6f, 0.45f, 0.1f); L.smoke = C(0.05f, 0.035f, 0.03f);
                    L.smokeLit = C(0.55f, 0.17f, 0.05f); L.ridgeFar = C(0.09f, 0.045f, 0.035f); L.ridgeNear = C(0.012f, 0.008f, 0.008f);
                    L.rim = C(1.4f, 0.35f, 0.06f); L.pillar = C(3f, 0.9f, 0.2f); L.seed = 1.7f;
                    L.spireTint = C(0.55f, 0.45f, 0.42f); L.spireVein = C(2.5f, 0.6f, 0.1f);
                    break;
                case 1: // The Molten Works: dark foundry brick, iron floor plate, rust walkways, hot rust trims
                    L.wallTex = "brick"; L.floorTex = "plate"; L.deckTex = "rust"; L.coverTex = "rust"; L.trimTex = "rust";
                    L.wallTint = C(1.0f, 0.62f, 0.5f); L.floorTint = C(0.75f, 0.5f, 0.42f); L.deckTint = C(1.0f, 0.8f, 0.7f);
                    L.coverTint = C(0.9f, 0.7f, 0.6f); L.trimTint = C(0.95f, 0.45f, 0.2f);
                    L.wallTile = 4f; L.floorTile = 3f; L.floorSmooth = 0.45f;
                    L.veinColor = C(1.8f, 0.4f, 0.05f);
                    L.lavaLight = C(1f, 0.32f, 0.08f); L.lavaIntensity = 19f;
                    L.ambientSky = C(0.07f, 0.05f, 0.07f); L.ambientEquator = C(0.075f, 0.03f, 0.02f); L.ambientGround = C(0.28f, 0.07f, 0.02f);
                    L.sun = C(1f, 0.3f, 0.15f); L.sunIntensity = 0.28f;
                    L.fog = C(0.2f, 0.05f, 0.025f); L.fogDensity = 0.0095f;
                    L.zenith = C(0.03f, 0.005f, 0.004f); L.glow = C(2.4f, 0.5f, 0.08f); L.smoke = C(0.06f, 0.02f, 0.015f);
                    L.smokeLit = C(0.8f, 0.15f, 0.03f); L.ridgeFar = C(0.14f, 0.04f, 0.02f); L.ridgeNear = C(0.02f, 0.006f, 0.005f);
                    L.rim = C(2f, 0.4f, 0.05f); L.pillar = C(4f, 1f, 0.15f); L.spires = 0.8f; L.seed = 5.3f; L.spireTex = "basalt";
                    L.spireTint = C(0.5f, 0.32f, 0.28f); L.spireVein = C(3.5f, 0.7f, 0.1f);
                    L.haze = C(0.6f, 0.12f, 0.02f);
                    break;
                default: // The Black Glass: black glassy rock walls, dark basalt floors (glossy), cold violet trims, embers
                    L.wallTex = "obsid"; L.floorTex = "basalt"; L.deckTex = "tiles"; L.coverTex = "obsid"; L.trimTex = "tiles";
                    L.wallTint = C(0.62f, 0.6f, 0.75f); L.floorTint = C(0.75f, 0.72f, 0.85f); L.deckTint = C(0.85f, 0.82f, 0.95f);
                    L.coverTint = C(0.7f, 0.66f, 0.82f); L.trimTint = C(0.5f, 0.42f, 0.62f);
                    L.floorSmooth = 0.62f; L.wallSmooth = 0.55f;
                    L.veinColor = C(1.6f, 0.4f, 0.08f);
                    L.lavaLight = C(1f, 0.38f, 0.12f); L.lavaIntensity = 15f;
                    L.brazierLight = C(0.75f, 0.5f, 1f);
                    L.ambientSky = C(0.05f, 0.04f, 0.1f); L.ambientEquator = C(0.035f, 0.028f, 0.05f); L.ambientGround = C(0.15f, 0.06f, 0.04f);
                    L.sun = C(0.55f, 0.4f, 0.9f); L.sunIntensity = 0.22f;
                    L.fog = C(0.05f, 0.03f, 0.07f); L.fogDensity = 0.0095f;
                    L.zenith = C(0.008f, 0.006f, 0.015f); L.glow = C(0.55f, 0.18f, 0.8f); L.smoke = C(0.02f, 0.015f, 0.03f);
                    L.smokeLit = C(0.22f, 0.08f, 0.28f); L.ridgeFar = C(0.05f, 0.035f, 0.07f); L.ridgeNear = C(0.005f, 0.004f, 0.008f);
                    L.rim = C(0.8f, 0.3f, 1.1f); L.pillar = C(2.4f, 0.6f, 0.12f); L.spires = 0.9f; L.embers = 0.3f; L.seed = 9.1f; L.spireTex = "obsid";
                    L.spireTint = C(0.35f, 0.33f, 0.45f); L.spireVein = C(1.6f, 0.4f, 0.08f);
                    L.haze = C(0.45f, 0.11f, 0.03f);
                    break;
            }
            L.bloomIntensity = 1.1f; L.bloomThreshold = 0.95f; L.vignette = 0.32f; L.exposure = 0.3f; L.aces = true;
            return L;
        }

        /// <summary>A Inked Inferno: the same surfaces painted (posterized, softer texture), the comic-ink bands and lines, a
        /// brighter painted ambient so the shade reads as a colour, neutral tonemapping.</summary>
        static void Inked(HellFloorLook L, int f)
        {
            L.bands = 1f; L.posterize = 0.5f; L.contrast = 0.85f; L.normal = 0.8f;
            L.sceneryInk = true; L.aces = false;
            L.ambientSky *= 1.9f; L.ambientEquator *= 1.9f; L.ambientGround *= 1.6f;
            L.wallTint *= 1.1f; L.floorTint *= 1.15f;
            L.lavaIntensity *= 1.05f; L.poolStrength = 1.15f;
            L.bloomIntensity = 0.6f; L.bloomThreshold = 1.1f; L.vignette = 0.22f; L.exposure = 0.15f;
            L.fogMode = FogMode.Linear; L.fogStart = 30f; L.fogEnd = 230f;
            if (f == 2) L.desaturate = 0.85f;   // the Black Glass colour-free (the kit's rule 4): only fire and the pall keep colour
        }

        /// <summary>C Black and Ember: colour-free near-black stone, only fire coloured, black fog; brighter fire so the floor and
        /// walls read in its light (the first C was unnavigable: the lights barely reached the stone).</summary>
        static void BlackAndEmber(HellFloorLook L, int f)
        {
            L.desaturate = 1f; L.contrast = 1.15f;
            Color g = new Color(0.42f, 0.42f, 0.42f);
            L.wallTint = g; L.floorTint = g * 1.15f; L.deckTint = g * 1.35f; L.coverTint = g * 1.1f; L.trimTint = new Color(0.5f, 0.5f, 0.5f);
            L.veinColor = new Color(1.8f, 0.42f, 0.06f);
            L.lavaLight = new Color(1f, 0.36f, 0.08f); L.lavaIntensity *= 1.3f; L.lavaRange *= 1.15f;
            L.brazierLight = new Color(1f, 0.5f, 0.18f); L.brazierIntensity *= 1.4f;
            L.sun = new Color(0.6f, 0.5f, 0.45f); L.sunIntensity = 0.06f;
            Color a = new Color(0.03f, 0.028f, 0.028f);
            L.ambientSky = a; L.ambientEquator = a; L.ambientGround = new Color(0.08f, 0.03f, 0.01f);
            L.carryLight = 3.2f;   // navigable: the floor at your feet always reads
            L.fog = new Color(0.012f, 0.008f, 0.007f); L.fogMode = FogMode.Linear; L.fogStart = 18f; L.fogEnd = 140f;
            L.zenith = new Color(0.003f, 0.002f, 0.002f); L.glow = new Color(0.9f, 0.22f, 0.04f); L.smoke = new Color(0.01f, 0.008f, 0.008f);
            L.smokeLit = new Color(0.22f, 0.05f, 0.01f); L.ridgeFar = new Color(0.025f, 0.018f, 0.016f); L.ridgeNear = new Color(0.003f, 0.002f, 0.002f);
            L.rim = new Color(1.3f, 0.3f, 0.05f); L.pillar = new Color(3f, 0.7f, 0.1f);
            L.spireTint = new Color(0.25f, 0.25f, 0.25f); L.spireVein = new Color(3f, 0.6f, 0.08f);
            L.haze = new Color(0.55f, 0.12f, 0.015f);
            L.bloomIntensity = 1.2f; L.bloomThreshold = 0.9f; L.vignette = 0.38f; L.exposure = 0.35f; L.aces = true;
        }
    }
}
