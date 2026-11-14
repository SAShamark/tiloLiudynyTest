Shader "HeartDemo/Proximity Vignette"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0
        _Direction ("Danger direction (screen, unit)", Vector) = (0, 0, 0, 0)
        _Focus ("Focus on direction", Range(0, 1)) = 0
        _Pulse ("Pulse", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float4 _Direction;
                float _Focus;
                float _Pulse;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 centred = (input.uv - 0.5) * 4.8;
                float radius = length(centred);
                float edge = smoothstep(0.42, 1.35, radius);

                // Glow gathers on the screen edge that faces the danger instead of ringing the whole view.
                float2 toPixel = centred / max(radius, 1e-4);
                float facing = saturate(dot(toPixel, _Direction.xy) * 0.5 + 0.5);
                float side = lerp(1.0, facing * facing * 1.6, _Focus);

                float glow = edge * side * (0.75 + 0.25 * _Pulse);
                half3 color = lerp(half3(0.85, 0.018, 0.025), half3(1.0, 0.25, 0.1), _Pulse * 0.4);
                return half4(color, saturate(glow * _Strength * 0.75));
            }
            ENDHLSL
        }
    }
}
