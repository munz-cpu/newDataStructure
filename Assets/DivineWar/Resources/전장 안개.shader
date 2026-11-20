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
                clip(SAMPLE_TEXTURE2D(_Exploration, sampler_Exploration, input.uv).a - 0.5);
                float2 patternUV = frac(input.uv * float2(12, 7));
                return half4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, patternUV).rgb, 1);
            }
            ENDHLSL
        }
    }
}
