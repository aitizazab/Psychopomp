// Hell/Sky: the world beyond the rooms (docs/HELL-LIGHTING.md), a procedural skybox. Above: a smoke ceiling lit from below,
// drifting; at the horizon: a fire glow, two ranges of jagged spires and crags in silhouette (far: hazy, near: black with a
// glowing rim), distant eruption columns; below the horizon: a lava sea's glow fading into the dark. All colours are per floor
// (HellBeyond sets them as material values when the camera changes floor). Fog stops at the sky, so _Horizon should match the
// floor's fog colour: the rooms' far walls then melt into the horizon glow.
Shader "Hell/Sky"
{
    Properties
    {
        _Zenith("Zenith", Color) = (0.02, 0.01, 0.01, 1)
        _Horizon("Horizon (= fog)", Color) = (0.25, 0.07, 0.03, 1)
        [HDR] _Glow("Horizon glow", Color) = (2.0, 0.5, 0.1, 1)
        _GlowHeight("Glow height", Float) = 0.12
        _Smoke("Smoke", Color) = (0.05, 0.03, 0.03, 1)
        [HDR] _SmokeLit("Smoke lit from below", Color) = (0.6, 0.15, 0.05, 1)
        _SmokeAmount("Smoke amount", Range(0, 1)) = 0.8
        _SmokeSpeed("Smoke speed", Float) = 0.01
        _RidgeFar("Far range", Color) = (0.12, 0.04, 0.03, 1)
        _RidgeNear("Near range", Color) = (0.01, 0.005, 0.005, 1)
        [HDR] _RimGlow("Near range rim", Color) = (1.5, 0.35, 0.06, 1)
        _RidgeHeight("Range height", Float) = 0.12
        _Spires("Spire density", Range(0, 1)) = 0.6
        [HDR] _Pillar("Eruption columns", Color) = (3, 0.8, 0.15, 1)
        _Embers("Embers", Range(0, 1)) = 0.5
        _Seed("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Zenith, _Horizon, _Glow, _Smoke, _SmokeLit, _RidgeFar, _RidgeNear, _RimGlow, _Pillar;
            float _GlowHeight, _SmokeAmount, _SmokeSpeed, _RidgeHeight, _Spires, _Embers, _Seed;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            V Vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.dir = i.positionOS.xyz;
                return o;
            }

            float H21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float N2(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(H21(i), H21(i + float2(1, 0)), f.x), lerp(H21(i + float2(0, 1)), H21(i + float2(1, 1)), f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float s = 0, a = 0.5;
                [unroll] for (int k = 0; k < 5; k++) { s += a * N2(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }
            // a periodic noise round the horizon: the azimuth on a circle
            float Ring(float az, float freq, float seed)
            {
                float2 c = float2(cos(az), sin(az)) * freq + seed;
                return Fbm(c);
            }

            half4 Frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float az = atan2(d.x, d.z);
                float t = _Time.y;

                // base gradient
                float up = saturate(h);
                half3 col = lerp(_Horizon.rgb, _Zenith.rgb, pow(up, 0.45));
                // the smoke ceiling: projected on a plane above, lit from below near the horizon
                if (h > 0.0)
                {
                    float2 sp = d.xz / (h + 0.08) * 0.9 + float2(t * _SmokeSpeed, t * _SmokeSpeed * 0.6) + _Seed;
                    float sm = Fbm(sp);
                    float sm2 = Fbm(sp * 2.7 - t * _SmokeSpeed * 1.7);
                    float dens = saturate((sm * 0.7 + sm2 * 0.45 - 0.42) * 2.2) * _SmokeAmount;
                    half3 smokeC = lerp(_SmokeLit.rgb, _Smoke.rgb, saturate(h * 2.2 + sm2 * 0.4));
                    col = lerp(col, smokeC, dens * saturate(h * 6.0 + 0.2));
                    // gaps in the smoke show a deeper glow
                    col += _SmokeLit.rgb * 0.25 * (1 - dens) * saturate(1 - h * 3.0);
                }
                // horizon glow
                float g = exp(-abs(h) / max(_GlowHeight, 0.01));
                float gv = 0.65 + 0.35 * Ring(az, 2.0, _Seed + 3.0);
                col += _Glow.rgb * g * gv;

                // eruption columns: a few azimuths with rising plumes
                float pillars = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float a0 = (H21(float2(k, _Seed)) * 2 - 1) * 3.14159;
                    float da = abs(atan2(sin(az - a0), cos(az - a0)));
                    float width = 0.012 + 0.03 * saturate(h * 3.0);
                    float top = 0.18 + 0.25 * H21(float2(k + 7, _Seed));
                    float flick = 0.7 + 0.3 * N2(float2(k * 3.1, t * 1.3 + h * 8.0));
                    pillars += exp(-da * da / (width * width)) * saturate(1 - h / top) * step(-0.02, h) * flick;
                }
                col += _Pillar.rgb * pillars * 0.6;

                // far range: low, hazy crags
                float far = _RidgeHeight * (0.25 + 0.5 * Ring(az, 3.0, _Seed + 11.0) + 0.35 * Ring(az, 22.0, _Seed + 13.0)) + 0.01;
                float aaF = fwidth(h) * 1.5 + 1e-4;
                float inFar = 1 - smoothstep(far - aaF, far + aaF, h);
                col = lerp(col, _RidgeFar.rgb + _Glow.rgb * 0.08 * g, inFar * step(-0.05, h));
                // near range: taller, with spires (sharp narrow peaks) and a glowing rim
                float base = _RidgeHeight * (0.1 + 0.4 * Ring(az, 5.0, _Seed + 23.0) + 0.3 * Ring(az, 30.0, _Seed + 29.0));
                float sp = Ring(az, 40.0, _Seed + 41.0);
                float spire = pow(saturate((sp - (1 - _Spires * 0.55)) * 3.2), 1.6) * _RidgeHeight * 2.4;
                float near = base + spire;
                float inNear = 1 - smoothstep(near - aaF, near + aaF, h);
                float rim = (1 - smoothstep(0, 0.012, abs(h - near))) * step(h, near + 0.012);
                col = lerp(col, _RidgeNear.rgb, inNear * step(-0.06, h));
                col += _RimGlow.rgb * rim * 0.25 * step(-0.06, h);
                // lava seams on the near range's face
                float seam = saturate((N2(float2(az * 60, h * 90)) - 0.82) * 6) * inNear * saturate(1 - (near - h) * 12);
                col += _RimGlow.rgb * seam * 0.8;

                // below the horizon: a lava sea's glow fading out
                if (h < 0.0)
                {
                    float sea = exp(h * 5.0);
                    col = lerp(_RidgeNear.rgb, col, sea);
                }

                // embers drifting up
                if (_Embers > 0.001)
                {
                    float2 ep = float2(az * 40.0, h * 40.0 - t * 0.6);
                    float2 cell = floor(ep);
                    float e = H21(cell + _Seed);
                    float2 f = frac(ep) - 0.5 - (float2(H21(cell + 3.1), H21(cell + 7.7)) - 0.5) * 0.6;
                    float spark = step(1 - 0.05 * _Embers, e) * (1 - smoothstep(0.02, 0.07, length(f)));
                    col += _Pillar.rgb * spark * 0.8 * saturate(1 - h * 1.5);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
