using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>The enemies' procedural textures (docs/HELL-LIGHTING.md), made once at run time (nothing to import): a charred
    /// hide (mottled grey with darker cracks between plates, for the albedo; the base colour tints it) and the same cracks as a
    /// white-on-black mask (for the ember emission). Both tile; the cracks are the edges of a wrapping Voronoi pattern.</summary>
    public static class HellLookTextures
    {
        const int N = 256, Cells = 7;
        static Texture2D _hide, _cracks;
        static float[] _edge;

        public static Texture2D Hide()
        {
            if (_hide != null) return _hide;
            var e = Edges();
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float n = 0.5f * TileNoise(u, v, 8) + 0.3f * TileNoise(u, v, 16) + 0.2f * TileNoise(u, v, 32);
                    float crack = 1f - Smooth(0.0f, 0.06f, e[y * N + x]);
                    float g = Mathf.Lerp(0.55f, 1.15f, n) * (1f - 0.6f * crack);
                    // a hint of warm in the plates' high points (heat under the char)
                    px[y * N + x] = new Color(g * 1.04f, g, g * 0.96f, 1f);
                }
            _hide = Make("HellLook_Hide", px);
            return _hide;
        }

        public static Texture2D Cracks()
        {
            if (_cracks != null) return _cracks;
            var e = Edges();
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float crack = 1f - Smooth(0.0f, 0.035f, e[y * N + x]);
                    // not every crack glows: a noise gate keeps it patchy, hotter in places
                    float gate = Smooth(0.35f, 0.6f, TileNoise(u + 0.37f, v + 0.11f, 4));
                    float k = crack * (0.25f + 0.75f * gate);
                    px[y * N + x] = new Color(k, k, k, 1f);
                }
            _cracks = Make("HellLook_Cracks", px);
            return _cracks;
        }

        static Texture2D Make(string name, Color[] px)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Per pixel: the distance to the nearest Voronoi cell edge (F2 - F1, in cell units), on a wrapping grid.</summary>
        static float[] Edges()
        {
            if (_edge != null) return _edge;
            var pts = new Vector2[Cells * Cells];
            for (int j = 0; j < Cells; j++)
                for (int i = 0; i < Cells; i++)
                    pts[j * Cells + i] = new Vector2(i + 0.15f + 0.7f * Hash(i, j), j + 0.15f + 0.7f * Hash(i + 91, j + 37));
            _edge = new float[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var p = new Vector2(x / (float)N * Cells, y / (float)N * Cells);
                    int cx = Mathf.FloorToInt(p.x), cy = Mathf.FloorToInt(p.y);
                    float d1 = 1e9f, d2 = 1e9f;
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int gx = cx + ox, gy = cy + oy;
                            int wx = ((gx % Cells) + Cells) % Cells, wy = ((gy % Cells) + Cells) % Cells;
                            var q = pts[wy * Cells + wx] + new Vector2(gx - wx, gy - wy);
                            float d = (q - p).magnitude;
                            if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                        }
                    _edge[y * N + x] = d2 - d1;
                }
            return _edge;
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Value noise on a <paramref name="cells"/> grid that wraps at 1 (so the texture tiles).</summary>
        public static float TileNoise(float u, float v, int cells)
        {
            u -= Mathf.Floor(u); v -= Mathf.Floor(v);
            float gx = u * cells, gy = v * cells;
            int x0 = Mathf.FloorToInt(gx), y0 = Mathf.FloorToInt(gy);
            float fx = gx - x0, fy = gy - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x1 = (x0 + 1) % cells, y1 = (y0 + 1) % cells;
            x0 %= cells; y0 %= cells;
            float a = Mathf.Lerp(Hash(x0, y0), Hash(x1, y0), fx);
            float b = Mathf.Lerp(Hash(x0, y1), Hash(x1, y1), fx);
            return Mathf.Lerp(a, b, fy);
        }
    }
}
