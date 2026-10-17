// Hell/Haze: additive light in the air (docs/HELL-LIGHTING.md). The heat glow rising off every lava basin (a curtain round the
// basin, brightest at the surface and fading over _Height), the sigils' light pillars, the braziers' halos. World-space height
// fade, a slow drifting shimmer, faded out near the camera (no screen-filling wash) and by the fog.
Shader "Hell/Haze"
{
    Properties
    {
        [HDR] _Color("Colour", Color) = (1.2, 0.35, 0.06, 1)
        _Base("Base height (world y)", Float) = -1.2
        _Height("Fade height (m)", Float) = 4
        _Power("Fade power", Float) = 1.6
        _Shimmer("Shimmer", Range(0, 1)) = 0.4
        _NearFade("Near fade (m)", Float) = 3
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Haze"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Base, _Height, _Power, _Shimmer, _NearFade;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half fog : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            V Vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs vp = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.fog = ComputeFogFactor(vp.positionCS.z);
                return o;
            }

            float H(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float N(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(H(i), H(i + float2(1, 0)), f.x), lerp(H(i + float2(0, 1)), H(i + float2(1, 1)), f.x), f.y);
            }

            half4 Frag(V i) : SV_Target
            {
                float3 p = i.positionWS;
                float t = saturate((p.y - _Base) / max(_Height, 0.01));
                float a = pow(1 - t, _Power);
                float s = N(float2(p.x * 0.35 + p.z * 0.35, p.y * 0.8 - _Time.y * 0.9));
                a *= lerp(1, 0.45 + 0.9 * s, _Shimmer);
                // glancing sheets are thin: fade where the view runs along the sheet's face less than where it looks through it
                float3 v = normalize(_WorldSpaceCameraPos - p);
                float facing = abs(dot(normalize(i.normalWS), v));
                a *= lerp(0.35, 1.0, facing);
                float dist = distance(_WorldSpaceCameraPos, p);
                a *= saturate((dist - 0.5) / max(_NearFade, 0.01));
                half3 c = _Color.rgb * a;
                // fog: additive light fades out (towards black) in fog, never adds fog colour
                c *= ComputeFogIntensity(i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
