Shader "VirtualFishing/FishMountainTriplanar"
{
    Properties
    {
        _MainTex ("Forest Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.66, 0.82, 0.45, 1)
        _TileScale ("Tile Scale", Float) = 0.18
        _BlendSharpness ("Blend Sharpness", Float) = 5
        _Brightness ("Brightness", Float) = 1.08
        _TextureStrength ("Texture Strength", Range(0, 1)) = 0.58
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

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float _TileScale;
                float _BlendSharpness;
                float _Brightness;
                float _TextureStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 weights = pow(abs(normal), max(_BlendSharpness, 0.001));
                weights /= max(weights.x + weights.y + weights.z, 0.001);

                float2 uvX = input.positionWS.zy * _TileScale;
                float2 uvY = input.positionWS.xz * _TileScale;
                float2 uvZ = input.positionWS.xy * _TileScale;

                float3 sampleX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX).rgb;
                float3 sampleY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY).rgb;
                float3 sampleZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ).rgb;
                float3 forest = sampleX * weights.x + sampleY * weights.y + sampleZ * weights.z;
                float luminance = dot(forest, float3(0.299, 0.587, 0.114));
                float detail = lerp(0.9, 1.2, saturate((luminance - 0.16) * 1.7));
                float3 mutedPhoto = lerp(luminance.xxx, forest, 0.52);

                float sun = saturate(dot(normal, normalize(float3(0.28, 0.82, 0.44))) * 0.42 + 0.58);
                float upward = saturate(normal.y * 0.9 + 0.18);
                float height = saturate((input.positionWS.y + 20.0) * 0.032);
                float ridge = saturate(sun * 0.55 + upward * 0.32 + height * 0.13);

                float3 valleyGreen = float3(0.22, 0.34, 0.17);
                float3 midForest = float3(0.35, 0.50, 0.23);
                float3 sunForest = float3(0.58, 0.67, 0.34);
                float3 baseForest = lerp(valleyGreen, midForest, upward);
                baseForest = lerp(baseForest, sunForest, ridge * 0.72);
                baseForest *= lerp(0.82, 1.05, sun);

                float3 photoColor = lerp(baseForest, mutedPhoto * _Tint.rgb * 1.18, saturate(_TextureStrength));
                float shadow = lerp(0.78, 1.04, upward) * lerp(0.86, 1.06, height);
                return half4(photoColor * detail * shadow * _Brightness, 1);
            }
            ENDHLSL
        }
    }
}
