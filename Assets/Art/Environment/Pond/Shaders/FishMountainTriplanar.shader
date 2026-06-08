Shader "VirtualFishing/FishMountainTriplanar"
{
    Properties
    {
        [MainTexture] _BaseMap ("Mountain Photo", 2D) = "white" {}
        [MainColor] _ForestTint ("Forest Tint", Color) = (0.34, 0.52, 0.24, 1)
        _RockTint ("Rock / Soil Tint", Color) = (0.34, 0.31, 0.24, 1)
        _HeightTint ("High Ridge Tint", Color) = (0.46, 0.52, 0.40, 1)
        _FogColor ("Distance Fog Color", Color) = (0.55, 0.66, 0.70, 1)

        _TextureScale ("Large Texture Scale", Range(0.005, 0.5)) = 0.075
        _DetailScale ("Detail Texture Scale", Range(0.02, 1.5)) = 0.26
        _BlendSharpness ("Triplanar Blend Sharpness", Range(1, 12)) = 4.5
        _PhotoStrength ("Photo Texture Strength", Range(0, 1)) = 0.78

        _HeightStart ("Height Start", Float) = -18
        _HeightRange ("Height Range", Float) = 58
        _Contrast ("Contrast", Range(0.5, 2)) = 1.08
        _Saturation ("Saturation", Range(0, 2)) = 1.05
        _Brightness ("Brightness", Range(0, 2)) = 1.05
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.16
        _SlopeRockStrength ("Slope Rock Strength", Range(0, 1)) = 0.38
        _HeightRockStrength ("Height Rock Strength", Range(0, 1)) = 0.24
        _FogBlend ("Distance Fog Blend", Range(0, 1)) = 0.18
        _LightDirection ("Light Direction", Vector) = (0.35, 0.82, 0.25, 0)

        _DebugMode ("Debug Mode", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _ForestTint;
                float4 _RockTint;
                float4 _HeightTint;
                float4 _FogColor;
                float4 _LightDirection;
                float _TextureScale;
                float _DetailScale;
                float _BlendSharpness;
                float _PhotoStrength;
                float _HeightStart;
                float _HeightRange;
                float _Contrast;
                float _Saturation;
                float _Brightness;
                float _NoiseStrength;
                float _SlopeRockStrength;
                float _HeightRockStrength;
                float _FogBlend;
                float _DebugMode;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float3 TriplanarWeights(float3 normalWS)
            {
                float3 weights = pow(abs(normalWS), max(_BlendSharpness, 0.001));
                return weights / max(weights.x + weights.y + weights.z, 0.001);
            }

            float3 SampleTriplanar(float3 positionWS, float3 normalWS, float scale)
            {
                float3 weights = TriplanarWeights(normalWS);
                float2 tiling = _BaseMap_ST.xy;
                float2 offset = _BaseMap_ST.zw;

                float2 uvX = positionWS.zy * scale * tiling + offset;
                float2 uvY = positionWS.xz * scale * tiling + offset;
                float2 uvZ = positionWS.xy * scale * tiling + offset;

                float3 sampleX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                float3 sampleY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb;
                float3 sampleZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;
                return sampleX * weights.x + sampleY * weights.y + sampleZ * weights.z;
            }

            float3 AdjustColor(float3 color)
            {
                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(luminance.xxx, color, _Saturation);
                color = (color - 0.5) * _Contrast + 0.5;
                return saturate(color * _Brightness);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = normalize(input.normalWS);
                float3 weights = TriplanarWeights(normalWS);

                if (_DebugMode > 0.5 && _DebugMode < 1.5)
                {
                    return half4(normalWS * 0.5 + 0.5, 1);
                }

                if (_DebugMode > 1.5 && _DebugMode < 2.5)
                {
                    return half4(weights, 1);
                }

                float3 largePhoto = SampleTriplanar(input.positionWS, normalWS, _TextureScale);
                float3 detailPhoto = SampleTriplanar(input.positionWS + normalWS * 1.37, normalWS, _DetailScale);
                float3 photo = lerp(largePhoto, detailPhoto, 0.34);

                if (_DebugMode > 2.5)
                {
                    return half4(photo, 1);
                }

                photo = AdjustColor(photo);
                float photoLum = dot(photo, float3(0.299, 0.587, 0.114));
                float canopyDetail = lerp(0.76, 1.28, saturate(photoLum));

                float height01 = saturate((input.positionWS.y - _HeightStart) / max(_HeightRange, 0.001));
                float slope01 = 1.0 - saturate(normalWS.y);
                float rockMask = saturate(pow(slope01, 1.45) * _SlopeRockStrength + height01 * _HeightRockStrength);

                float3 forestBase = lerp(_ForestTint.rgb * 0.72, _ForestTint.rgb * 1.12, saturate(normalWS.y * 0.85 + 0.15));
                forestBase = lerp(forestBase, _HeightTint.rgb, height01 * 0.42);
                float3 terrainBase = lerp(forestBase, _RockTint.rgb, rockMask);

                float noise = ValueNoise(input.positionWS.xz * 0.045 + input.positionWS.y * 0.012);
                float broadNoise = ValueNoise(input.positionWS.xz * 0.018 + 19.7);
                float variation = lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, noise);
                terrainBase = lerp(terrainBase, terrainBase * float3(0.74, 0.88, 0.70), broadNoise * _NoiseStrength);

                float3 photoColor = lerp(terrainBase * canopyDetail, photo, _PhotoStrength * 0.30);
                float3 color = lerp(terrainBase, photoColor, _PhotoStrength) * variation;

                float3 lightDir = normalize(_LightDirection.xyz);
                float lambert = saturate(dot(normalWS, lightDir));
                float shade = lerp(0.58, 1.16, lambert);
                color *= shade;

                float distanceToCamera = distance(input.positionWS, _WorldSpaceCameraPos.xyz);
                float fog = saturate((distanceToCamera - 42.0) / 170.0) * _FogBlend;
                color = lerp(color, _FogColor.rgb, fog);

                return half4(saturate(color), 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
