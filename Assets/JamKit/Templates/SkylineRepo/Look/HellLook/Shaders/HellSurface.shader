// Hell/Surface: the rooms' rock, ash, tiles and iron (docs/HELL-LIGHTING.md). One shader for all three art directions.
// - World-space TRIPLANAR textures (the room boxes are scaled cubes with no useful UVs): the TOP set on up-facing surfaces
//   (floors, ledges), the SIDE set on walls and undersides, so a wall never shares a floor's texture. Normal maps blended with
//   Ben Golus' whiteout blend.
// - An edge TRIM from the object-space position (the boxes are unit cubes): a band along the top of every wall and a curb
//   round every floor slab's rim, in _TrimTint, so walls, ledges and walkway edges read at a glance.
// - VEINS: the side texture's crevices glow below _VeinTop (rock heated by the lava, spires' lava seams).
// - Two lighting models: _Bands 0 = URP's PBR (UniversalFragmentPBR: Forward+ light loop, shadows, SSAO, fog), for B and C;
//   _Bands 1 = the comic-ink bands (direction A): the sun in three bands and every additional light as banded pools whose
//   reach follows the light's INTENSITY (InkLit's pools only followed distance, so a 9 m lava light lit a 2-5 m spot).
Shader "Hell/Surface"
{
    Properties
    {
        _TopMap("Top (floors) albedo", 2D) = "grey" {}
        [Normal] _TopNormal("Top normal", 2D) = "bump" {}
        _SideMap("Side (walls) albedo", 2D) = "grey" {}
        [Normal] _SideNormal("Side normal", 2D) = "bump" {}
        _TopTint("Top tint", Color) = (1, 1, 1, 1)
        _SideTint("Side tint", Color) = (1, 1, 1, 1)
        _TrimTint("Trim tint", Color) = (0.5, 0.35, 0.3, 1)
        _TopTile("Top tile (m)", Float) = 4
        _SideTile("Side tile (m)", Float) = 5
        _NormalStrength("Normal strength", Range(0, 2)) = 1
        _TopSmooth("Top smoothness", Range(0, 1)) = 0.2
        _SideSmooth("Side smoothness", Range(0, 1)) = 0.25
        _Desaturate("Desaturate", Range(0, 1)) = 0
        _Posterize("Posterize (painted)", Range(0, 1)) = 0
        _Bands("Ink bands (A)", Float) = 0
        _Contrast("Texture contrast", Range(0.3, 2.5)) = 1
        _TrimWidth("Trim width (m)", Float) = 0.35
        _TrimOn("Trim on", Float) = 1
        [HDR] _VeinColor("Vein glow", Color) = (0, 0, 0, 1)
        _VeinTop("Veins below (world y)", Float) = -0.3
        _VeinFade("Vein fade (m)", Float) = 1.2
        _GradBottom("Darker towards the floor", Range(0, 1)) = 0.7
        _GradHeight("Gradient height (m)", Float) = 3
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 1)

        [HideInInspector] _BaseMap("Base", 2D) = "white" {}
        [HideInInspector] _BaseColor("Base colour", Color) = (1, 1, 1, 1)
        [HideInInspector] _Cutoff("Cutoff", Float) = 0.5
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "Assets/JamKit/Templates/SkylineRepo/Look/HellLook/Shaders/HellSurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // A global set by HellLightRig: x = pool strength in the banded model (direction A).
            float4 _SkHellLight;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 normalOS   : TEXCOORD3;
                half fogFactor    : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = vp.positionCS;
                o.positionWS = vp.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionOS = input.positionOS.xyz;
                o.normalOS = input.normalOS;
                o.fogFactor = ComputeFogFactor(vp.positionCS.z);
                return o;
            }

            half Lum(half3 c) { return dot(c, half3(0.2126h, 0.7152h, 0.0722h)); }

            void Frag(Varyings input, out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 p = input.positionWS;
                half3 n = normalize(input.normalWS);

                // ---- triplanar sampling (whiteout normal blend)
                half3 w = pow(abs(n), 4.0h);
                w /= max(w.x + w.y + w.z, 1e-4h);
                float2 uvX = p.zy / _SideTile, uvZ = p.xy / _SideTile;
                float2 uvY = p.xz / (n.y > 0 ? _TopTile : _SideTile);
                half up = n.y > 0 ? 1.0h : 0.0h;

                half3 cX = SAMPLE_TEXTURE2D(_SideMap, sampler_SideMap, uvX).rgb;
                half3 cZ = SAMPLE_TEXTURE2D(_SideMap, sampler_SideMap, uvZ).rgb;
                half3 cYTop = SAMPLE_TEXTURE2D(_TopMap, sampler_TopMap, uvY).rgb;
                half3 cYSide = SAMPLE_TEXTURE2D(_SideMap, sampler_SideMap, uvY).rgb;
                half3 sideRaw = cX * w.x + cZ * w.z + cYSide * w.y * (1.0h - up);
                half sideW = w.x + w.z + w.y * (1.0h - up);
                half3 topRaw = cYTop;
                half topW = w.y * up;

                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideNormal, uvX), _NormalStrength);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideNormal, uvZ), _NormalStrength);
                half3 nYT = UnpackNormalScale(SAMPLE_TEXTURE2D(_TopNormal, sampler_TopNormal, uvY), _NormalStrength);
                half3 nYS = UnpackNormalScale(SAMPLE_TEXTURE2D(_SideNormal, sampler_SideNormal, uvY), _NormalStrength);
                half3 nY = lerp(nYS, nYT, up);
                nX = half3(nX.xy + n.zy, abs(nX.z) * n.x);
                nY = half3(nY.xy + n.xz, abs(nY.z) * n.y);
                nZ = half3(nZ.xy + n.xy, abs(nZ.z) * n.z);
                half3 normalWS = normalize(nX.zyx * w.x + nY.xzy * w.y + nZ.xyz * w.z);

                // texture contrast about its own mean, then tint
                half3 sideC = pow(max(sideRaw, 1e-4h), _Contrast) * _SideTint.rgb;
                half3 topC = pow(max(topRaw, 1e-4h), _Contrast) * _TopTint.rgb;
                half3 albedo = (sideC * sideW + topC * topW) / max(sideW + topW, 1e-4h);

                // ---- the trim (object space: unit cube)
                float3 scale = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21), length(unity_ObjectToWorld._m02_m12_m22));
                half3 nOS = normalize(input.normalOS);
                float3 pOS = input.positionOS;
                half trim = 0;
                if (_TrimOn > 0.5h)
                {
                    float toTop = (0.5 - pOS.y) * scale.y;
                    float toRim = min((0.5 - abs(pOS.x)) * scale.x, (0.5 - abs(pOS.z)) * scale.z);
                    half isSide = 1.0h - step(0.5h, abs(nOS.y));
                    half isTop = step(0.5h, nOS.y);
                    float aaT = max(fwidth(toTop), 1e-4);
                    float aaR = max(fwidth(toRim), 1e-4);
                    trim = isSide * (1.0h - smoothstep(_TrimWidth - aaT, _TrimWidth + aaT, toTop))
                         + isTop * (1.0h - smoothstep(_TrimWidth * 0.7 - aaR, _TrimWidth * 0.7 + aaR, toRim));
                    // a thin worn bright line right on the corner
                    half lip = isSide * (1.0h - smoothstep(0.0, 0.05 + aaT, toTop)) + isTop * (1.0h - smoothstep(0.0, 0.05 + aaR, toRim));
                    albedo = lerp(albedo, Lum(sideRaw).xxx * 1.6h * _TrimTint.rgb + _TrimTint.rgb * 0.25h, saturate(trim));
                    albedo += lip * 0.12h * _TrimTint.rgb;
                }

                // darker towards the floor (contact), lighter up the wall
                half grad = lerp(_GradBottom, 1.0h, saturate(p.y / max(_GradHeight, 0.01h)));
                albedo *= lerp(1.0h, grad, saturate(sideW));

                // painted (A): soft posterize of the value, the hue kept
                if (_Posterize > 0.001h)
                {
                    half l = Lum(albedo);
                    half lq = (floor(l * 9.0h) + 0.5h) / 9.0h;
                    albedo = lerp(albedo, albedo * (lq / max(l, 1e-3h)), _Posterize);
                }
                half lumA = Lum(albedo);
                albedo = lerp(albedo, lumA.xxx, _Desaturate);

                // veins: crevices (the dark of the side texture) glow below _VeinTop
                half crev = saturate((0.07h - Lum(sideRaw)) * 14.0h) * sideW;
                half veinMask = saturate((_VeinTop - p.y) / max(_VeinFade, 0.01h) + 1.0h) * crev;
                half3 emission = _VeinColor.rgb * veinMask + _EmissionColor.rgb;

                half smooth = lerp(_SideSmooth, _TopSmooth, saturate(topW));

                InputData inputData = (InputData)0;
                inputData.positionWS = p;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(p);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(p);
            #endif
                inputData.fogCoord = InitializeInputDataFog(float4(p, 1.0), input.fogFactor);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half3 color;
                if (_Bands < 0.5h)
                {
                    SurfaceData s = (SurfaceData)0;
                    s.albedo = albedo;
                    s.metallic = 0;
                    s.specular = 0;
                    s.smoothness = smooth;
                    s.occlusion = 1;
                    s.emission = emission;
                    s.alpha = 1;
                    s.normalTS = half3(0, 0, 1);
                    color = UniversalFragmentPBR(inputData, s).rgb;
                }
                else
                {
                    // ---- the comic-ink bands (direction A)
                    uint meshRenderingLayers = GetMeshRenderingLayer();
                    half4 shadowMask = inputData.shadowMask;
                    Light mainLight = GetMainLight(inputData.shadowCoord, p, shadowMask);
                    half x = saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation;
                    half fw = max(fwidth(x), 0.002h);
                    half sunI = 0.1h + 0.3h * smoothstep(0.08h - fw, 0.08h + fw, x) + 0.6h * smoothstep(0.45h - fw, 0.45h + fw, x);
                    color = albedo * (inputData.bakedGI + mainLight.color * sunI);
                    half3 add = 0;
                    half k = _SkHellLight.x > 0 ? (half)_SkHellLight.x : 1.0h;
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        uint lightIndex = dirIndex;
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        Light l2 = GetAdditionalLight(lightIndex, p, shadowMask);
                        half pk2 = max(l2.color.r, max(l2.color.g, l2.color.b));
                        half e2 = l2.distanceAttenuation * l2.shadowAttenuation * saturate(dot(normalWS, l2.direction)) * pk2;
                        add += (l2.color / max(pk2, 1e-4h)) * (smoothstep(0.04h, 0.06h, e2) * 0.35h + smoothstep(0.2h, 0.24h, e2) * 0.45h + smoothstep(0.7h, 0.8h, e2) * 0.6h);
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light l = GetAdditionalLight(lightIndex, p, shadowMask);
                    #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(l.layerMask, meshRenderingLayers))
                    #endif
                        {
                            half pk = max(l.color.r, max(l.color.g, l.color.b));
                            half e = l.distanceAttenuation * l.shadowAttenuation * saturate(dot(normalWS, l.direction)) * pk;
                            half band = smoothstep(0.04h, 0.06h, e) * 0.35h + smoothstep(0.2h, 0.24h, e) * 0.45h + smoothstep(0.7h, 0.8h, e) * 0.6h;
                            add += (l.color / max(pk, 1e-4h)) * band;
                        }
                    LIGHT_LOOP_END
                #endif
                    color += albedo * add * k + emission;
                }
                color = MixFog(color, inputData.fogCoord);
                outColor = half4(color, 1.0h);
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Assets/JamKit/Templates/SkylineRepo/Look/HellLook/Shaders/HellSurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Assets/JamKit/Templates/SkylineRepo/Look/HellLook/Shaders/HellSurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #include "Assets/JamKit/Templates/SkylineRepo/Look/HellLook/Shaders/HellSurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
