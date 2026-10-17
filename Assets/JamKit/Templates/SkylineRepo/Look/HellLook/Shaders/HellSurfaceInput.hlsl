// Hell/Surface: the material's inputs, shared by every pass (one UnityPerMaterial CBUFFER, or the SRP Batcher breaks).
// docs/HELL-LIGHTING.md. _BaseMap / _BaseColor are kept (URP's shadow / depth pass files read them).
#ifndef HELL_SURFACE_INPUT_INCLUDED
#define HELL_SURFACE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

TEXTURE2D(_TopMap);     SAMPLER(sampler_TopMap);
TEXTURE2D(_TopNormal);  SAMPLER(sampler_TopNormal);
TEXTURE2D(_SideMap);    SAMPLER(sampler_SideMap);
TEXTURE2D(_SideNormal); SAMPLER(sampler_SideNormal);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _TopTint;
half4 _SideTint;
half4 _TrimTint;
half4 _VeinColor;
half4 _EmissionColor;
float _TopTile;
float _SideTile;
half _NormalStrength;
half _TopSmooth;
half _SideSmooth;
half _Desaturate;
half _Posterize;
half _Bands;
half _Contrast;
half _TrimWidth;
half _TrimOn;
half _VeinTop;
half _VeinFade;
half _GradBottom;
half _GradHeight;
half _Cutoff;
half _Cull;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

#endif
