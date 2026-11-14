// Procedural blood-vessel wall seen from inside: noisy tissue with veins drifting along the flow,
// a pulse wave travelling down the tube, a soft "headlight" falloff and distance fog.
Shader "Game/VesselWall"
{
    Properties
    {
        _BaseColor ("Tissue", Color) = (0.42, 0.04, 0.06, 1)
        _HighlightColor ("Tissue Highlight", Color) = (0.85, 0.2, 0.22, 1)
        _VeinColor ("Veins", Color) = (0.2, 0.0, 0.03, 1)
        _NoiseScale ("Noise Scale", Float) = 1.6
        _VeinScale ("Vein Scale", Float) = 0.7
        _FlowSpeed ("Flow Speed", Float) = 0.35
        _PulseAmplitude ("Pulse Amplitude", Float) = 0.12
        _PulsePeriod ("Pulse Period", Float) = 0.9
        _PulseWavelength ("Pulse Wavelength", Float) = 7
        _FogColor ("Fog", Color) = (0.06, 0.004, 0.01, 1)
        _FogStart ("Fog Start", Float) = 4
        _FogEnd ("Fog End", Float) = 30
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "VesselWall"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
                float pulse : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _HighlightColor;
                half4 _VeinColor;
                float _NoiseScale;
                float _VeinScale;
                float _FlowSpeed;
                float _PulseAmplitude;
                float _PulsePeriod;
                float _PulseWavelength;
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // uv.y is the distance along the tube in metres, so the wave travels away from the viewer.
                float phase = _Time.y / _PulsePeriod - input.uv.y / _PulseWavelength;
                float wave = pow(saturate(sin(phase * 6.2832)), 3.0);
                float3 positionOS = input.positionOS.xyz + input.normalOS * wave * _PulseAmplitude;

                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.pulse = wave;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Wrap the angular coordinate on a circle so the noise has no seam.
                float angle = input.uv.x * 6.2832;
                float along = input.uv.y - _Time.y * _FlowSpeed;
                float3 p = float3(cos(angle) * 2.0, sin(angle) * 2.0, along);

                float tissue = VesselNoise(p * _NoiseScale) * 0.6 + VesselNoise(p * _NoiseScale * 2.7) * 0.3
                    + VesselNoise(p * _NoiseScale * 6.1) * 0.1;
                float veins = 1.0 - abs(VesselNoise(p * _VeinScale + 11.3) * 2.0 - 1.0);
                veins = smoothstep(0.82, 0.97, veins);

                half3 color = lerp(_BaseColor.rgb, _HighlightColor.rgb, tissue * tissue);
                color = lerp(color, _VeinColor.rgb, veins * 0.8);

                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float facing = abs(dot(normalize(input.normalWS), view));
                color *= 0.35 + 0.65 * facing;
                color *= 1.0 + input.pulse * 0.25;

                color = ApplyVesselFog(color, input.positionWS, _FogColor.rgb, _FogStart, _FogEnd);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
