// Organ projection that starts as an additive hologram (tinted texture, fresnel rim, scanlines)
// and can materialise into a solid, lit organ through _Fill with a glowing dissolve edge.
// _Glitch drives vertex slice jitter + flicker, _Visibility fades/flashes the whole thing.
// Premultiplied blending: alpha 0 behaves additively (hologram), alpha 1 is opaque (solid organ).
Shader "Game/Hologram"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Color ("Hologram Tint", Color) = (0.35, 0.9, 1, 1)
        _TextureInfluence ("Texture Influence", Range(0, 1)) = 0.65
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.5
        _FresnelBoost ("Fresnel Boost", Range(0, 4)) = 1.6
        _ScanDensity ("Scanline Density", Float) = 90
        _ScanSpeed ("Scanline Speed", Float) = 0.6
        _Alpha ("Hologram Alpha", Range(0, 1)) = 0.8
        _SolidTint ("Solid Tint", Color) = (1, 1, 1, 1)
        _DissolveScale ("Dissolve Noise Scale", Float) = 9
        _EdgeWidth ("Dissolve Edge Width", Range(0.01, 0.3)) = 0.08
        _Fill ("Fill", Range(0, 1)) = 0
        _Glitch ("Glitch", Range(0, 1)) = 0
        _Visibility ("Visibility", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "VesselFog.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;
                half _TextureInfluence;
                half _FresnelPower;
                half _FresnelBoost;
                float _ScanDensity;
                float _ScanSpeed;
                half _Alpha;
                half4 _SolidTint;
                float _DissolveScale;
                half _EdgeWidth;
                half _Fill;
                half _Glitch;
                half _Visibility;
            CBUFFER_END

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float frame = floor(_Time.y * 14.0);
                float slice = floor(positionOS.y * 24.0) + frame * 7.0;
                float active = step(0.72, Hash(slice + 13.1));
                positionOS.x += (Hash(slice) - 0.5) * active * _Glitch * 0.18;

                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(1.0 - saturate(abs(dot(normal, view))), _FresnelPower) * _FresnelBoost;

                // Hologram look.
                half3 holoBase = lerp(_Color.rgb, texel * _Color.rgb * 2.0, _TextureInfluence);
                half scan = 0.72 + 0.28 * sin(input.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed * 6.2832);
                half flicker = 1.0 - _Glitch * 0.65 * step(0.45, Hash(floor(_Time.y * 30.0)));
                half3 holo = (holoBase + _Color.rgb * fresnel) * scan * flicker;
                half holoAlpha = saturate((_Alpha + fresnel * 0.5) * scan);

                // Solid, lit look.
                Light mainLight = GetMainLight();
                half wrap = saturate(dot(normal, mainLight.direction) * 0.6 + 0.4);
                half3 ambient = SampleSH(normal);
                half3 solid = texel * _SolidTint.rgb * (mainLight.color * wrap + ambient);
                solid += _Color.rgb * fresnel * 0.15;

                // Noise dissolve between the two, with a glowing seam.
                float noise = VesselNoise(input.positionOS * _DissolveScale) * 0.7 + VesselNoise(input.positionOS * _DissolveScale * 2.3) * 0.3;
                float threshold = _Fill * (1.0 + _EdgeWidth * 2.0) - _EdgeWidth;
                half solidMask = smoothstep(noise - _EdgeWidth * 0.5, noise + _EdgeWidth * 0.5, threshold);
                half edge = saturate(1.0 - abs(threshold - noise) / _EdgeWidth) * step(0.001, _Fill) * step(_Fill, 0.999);

                half3 color = lerp(holo * holoAlpha, solid, solidMask) + _Color.rgb * edge * 1.5;
                half alpha = solidMask;
                return half4(color, alpha) * _Visibility;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
