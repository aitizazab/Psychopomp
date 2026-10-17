// Hell/Halo: the glow a fire throws into smoky air (docs/HELL-LIGHTING.md): a camera-facing quad, additive, a soft radial
// falloff with a hot centre, faded by the fog and near the camera. Put on Unity's Quad mesh; the object's scale is the size.
Shader "Hell/Halo"
{
    Properties
    {
        [HDR] _Color("Colour", Color) = (1.5, 0.5, 0.12, 1)
        _Falloff("Falloff", Float) = 2.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "DisableBatching" = "True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            Name "Halo"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Falloff;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; float dist : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            V Vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 centre = TransformObjectToWorld(float3(0, 0, 0));
                float size = length(unity_ObjectToWorld._m00_m10_m20);
                float3 right = UNITY_MATRIX_V[0].xyz, up = UNITY_MATRIX_V[1].xyz;
                float3 p = centre + (right * i.positionOS.x + up * i.positionOS.y) * size;
                o.positionCS = TransformWorldToHClip(p);
                o.uv = i.positionOS.xy * 2;
                o.fog = ComputeFogFactor(o.positionCS.z);
                o.dist = distance(_WorldSpaceCameraPos, centre);
                return o;
            }

            half4 Frag(V i) : SV_Target
            {
                float r = saturate(1 - length(i.uv));
                float a = pow(r, _Falloff) + pow(r, 12) * 0.6;
                a *= saturate((i.dist - 1.0) / 3.0);
                half3 c = _Color.rgb * a * ComputeFogIntensity(i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
