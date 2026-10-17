using JamKit.Skyline;
using UnityEngine;
using UnityEngine.UIElements;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// Hell's health (docs/HELL-UI.md, "Health: the ember vessel"), drawn into <see cref="SkylineHudHealth"/>'s element when
    /// <see cref="HellHudStyle.Active"/>. The damage model is the kit's, unchanged: the chassis is your health, the plating an
    /// outer layer that takes hits first and comes back from fresh kills (walk over a wreck). Re-skinned, no text:
    /// <list type="bullet">
    /// <item><b>The vessel</b> (health): a slanted obsidian plate with an ember rim, full of molten ember. As it drains, dark
    /// cracks open across the emptied part (<see cref="HellVesselModel.Cracks"/>). The heart-gem at its left end glows with
    /// the ember and throbs under 35%, with the screen's red edge (the Skyline rule).</item>
    /// <item><b>The Ward</b> (the plating): a thin bone slab over the vessel, offset up and left as before. It shatters into
    /// shards when it breaks.</item>
    /// </list>
    /// Geometry in the element's own units (the layout's <see cref="HellHudPlaces.Vessel"/> box, 420 x 92).
    /// </summary>
    public static class HellHudVessel
    {
        public const float GemX = 26f, GemY = 58f, GemR = 22f;
        public const float BodyX = 54f, BodyY = 44f, BodyW = 330f, BodyH = 28f, Skew = 12f, Inset = 4f;
        public const float WardX = 48f, WardY = 24f, WardH = 12f;
        static readonly float WardSkew = Skew * WardH / BodyH;

        public static void Draw(Painter2D p, HealthBarModel m, bool hasWard, float shatter01, bool flash)
        {
            float cf = Mathf.Clamp01(m.ChassisFrac), pf = Mathf.Clamp01(m.PlatingFrac), pulse = m.LowPulse;
            var ember = Color.Lerp(HellHudStyle.Ember, HellHudStyle.EmberHot, pulse);
            if (flash) ember = Color.white;

            // the vessel: obsidian, ember rim
            HellHudStyle.PlateShape(p, BodyX, BodyY, BodyW, BodyH, Skew, HellHudStyle.Plate, HellHudStyle.EmberEdge);
            // the molten ember, inset
            float iw = BodyW - Inset * 2f, ih = BodyH - Inset * 2f, isk = Skew * ih / BodyH;
            if (cf > 0f)
            {
                SkylineHudStyle.Parallelogram(p, BodyX + Inset + (Skew - isk) * 0.5f, BodyY + Inset, iw * cf, ih, isk);
                p.fillColor = ember;
                p.Fill();
                // the hot core along the top edge
                SkylineHudStyle.Parallelogram(p, BodyX + Inset + Skew * 0.5f, BodyY + Inset, iw * cf, 5f, isk * 5f / ih);
                p.fillColor = new Color(1f, 0.9f, 0.6f, 0.55f);
                p.Fill();
            }
            // the cracks, in the drained part (right to left as it empties)
            int cracks = HellVesselModel.Cracks(cf);
            p.strokeColor = HellHudStyle.Crack;
            p.lineWidth = 2.5f;
            p.lineJoin = LineJoin.Miter;
            for (int k = 0; k < cracks; k++)
            {
                float f = 1f - (k + 0.5f) / 6f;                       // where this sixth of the vessel sits
                float x = BodyX + BodyW * f;
                float jag = (k & 1) == 0 ? 5f : -4f;
                p.BeginPath();
                p.MoveTo(new Vector2(x + Skew, BodyY + 1f));
                p.LineTo(new Vector2(x + Skew * 0.6f + jag, BodyY + BodyH * 0.38f));
                p.LineTo(new Vector2(x + Skew * 0.35f - jag, BodyY + BodyH * 0.66f));
                p.LineTo(new Vector2(x, BodyY + BodyH - 1f));
                p.Stroke();
                // a branch
                p.BeginPath();
                p.MoveTo(new Vector2(x + Skew * 0.6f + jag, BodyY + BodyH * 0.38f));
                p.LineTo(new Vector2(x + Skew * 0.6f + jag + 9f, BodyY + BodyH * 0.25f));
                p.Stroke();
            }

            // the heart-gem: a diamond, ember while alive, throbbing when low
            float glow = cf > 0f ? 1f : 0f;
            var gem = cf > 0f ? Color.Lerp(HellHudStyle.Ember, HellHudStyle.EmberHot, 0.35f + 0.65f * pulse) : HellHudStyle.Spent;
            if (pulse > 0f)
            {
                p.BeginPath();
                p.Arc(new Vector2(GemX, GemY), GemR + 4f + 6f * pulse, new Angle(0f), new Angle(360f));
                p.fillColor = new Color(1f, 0.35f, 0.08f, 0.25f * pulse * glow);
                p.Fill();
            }
            HellHudStyle.Diamond(p, new Vector2(GemX, GemY), GemR, 1f, gem, HellHudStyle.Plate);
            HellHudStyle.Diamond(p, new Vector2(GemX, GemY), GemR * 0.45f, cf > 0f ? 1f : 0f, new Color(1f, 0.95f, 0.8f, 1f), HellHudStyle.Plate);

            // the Ward: a bone slab above
            if (hasWard)
            {
                SkylineHudStyle.Parallelogram(p, WardX, WardY, BodyW, WardH, WardSkew);
                p.fillColor = new Color(HellHudStyle.Ink.r, HellHudStyle.Ink.g, HellHudStyle.Ink.b, 0.55f);
                p.Fill();
                if (pf > 0f)
                {
                    SkylineHudStyle.Parallelogram(p, WardX, WardY, BodyW * pf, WardH, WardSkew);
                    p.fillColor = flash ? Color.white : HellHudStyle.Bone;
                    p.Fill();
                }
                SkylineHudStyle.Parallelogram(p, WardX, WardY, BodyW, WardH, WardSkew);
                p.strokeColor = HellHudStyle.Ink;
                p.lineWidth = SkylineHudStyle.KeylinePx;
                p.Stroke();
            }

            // the Ward breaking: SkylineHudHealth's shard maths, mapped onto the slab
            if (shatter01 >= 0f)
            {
                float sx = BodyW / SkylineHudHealth.BarW, sy = WardH / SkylineHudHealth.PlateH;
                for (int i = 0; i < SkylineHudHealth.ShardCount; i++)
                {
                    var c = SkylineHudHealth.Shard(i, shatter01, out float alpha);
                    p.BeginPath();
                    for (int k = 0; k < 4; k++)
                    {
                        var v = new Vector2(WardX + (c[k].x - 6f) * sx, WardY + (c[k].y - 5f) * sy);
                        if (k == 0) p.MoveTo(v); else p.LineTo(v);
                    }
                    p.ClosePath();
                    var fill = HellHudStyle.Bone;
                    fill.a = alpha;
                    var ink = HellHudStyle.Ink;
                    ink.a = alpha;
                    p.fillColor = fill;
                    p.Fill();
                    p.strokeColor = ink;
                    p.lineWidth = 2f;
                    p.Stroke();
                }
            }
        }
    }
}
