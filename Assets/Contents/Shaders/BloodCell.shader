// Opaque red blood cell for mesh particles: per-particle tint from the vertex colour,
// soft wrap lighting from the viewer's side, rim darkening and the shared vessel fog.
Shader "Game/BloodCell"
{
    Properties
    {
        _BaseColor ("Colour", Color) = (0.78, 0.08, 0.1, 1)
        _ShadowColor ("Shadow", Color) = (0.25, 0.01, 0.03, 1)
        _Gloss ("Gloss", Range(0, 1)) = 0.35
        _FogColor ("Fog", Color) = (0.06, 0.004, 0.01, 1)
        _FogStart ("Fog Start", Float) = 4
        _FogEnd ("Fog End", Float) = 30
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "BloodCell"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "VesselFog.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half _Gloss;
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                // Particle systems bake the world transform into the vertices.
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 light = normalize(view + float3(0.2, 0.6, 0.0));

                half wrap = saturate(dot(normal, light) * 0.5 + 0.5);
                half3 color = lerp(_ShadowColor.rgb, _BaseColor.rgb, wrap) * input.color.rgb;
                half specular = pow(saturate(dot(normal, normalize(light + view))), 24.0) * _Gloss;
                half rim = pow(1.0 - saturate(abs(dot(normal, view))), 2.0);
                color = color * (1.0 - rim * 0.45) + specular;

                color = ApplyVesselFog(color, input.positionWS, _FogColor.rgb, _FogStart, _FogEnd);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
