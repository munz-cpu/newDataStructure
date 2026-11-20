Shader "Divine War/Battle Fog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Fog Pattern", 2D) = "white" {}
        [NoScaleOffset] _Exploration ("Exploration", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes { COMMON_2D_INPUTS };
            struct Varyings { COMMON_2D_OUTPUTS };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Exploration);
            SAMPLER(sampler_Exploration);

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half edge = SAMPLE_TEXTURE2D(_Exploration, sampler_LinearClamp, input.uv).a;
                half opacity = smoothstep(0.0, 0.55, edge);
                clip(opacity - 0.001);

                float2 drift = _Time.y * float2(0.003, 0.001);
                float2 uv = input.uv * float2(1.6, 2.2);
                half distant = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + drift).g;
                half nearMist = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * 1.7 - drift).g;
                half density = saturate((distant + nearMist) * 0.7);
                return half4(lerp(half3(0.085, 0.12, 0.14), half3(0.19, 0.24, 0.25), density), opacity);
            }
            ENDHLSL
        }
    }
}
