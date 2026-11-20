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
                half cell = SAMPLE_TEXTURE2D(_Exploration, sampler_Exploration, input.uv).a;
                half softEdge = SAMPLE_TEXTURE2D(_Exploration, sampler_LinearClamp, input.uv).a;
                // Extend the soft edge into explored ground while unexplored cells stay opaque.
                half opacity = lerp(cell, 1.0, smoothstep(0.0, 0.30, softEdge - cell));
                clip(opacity - 0.001);

                float2 mistUV = input.uv * float2(3.6, 3.2);
                float2 drift = _Time.y * float2(0.009, 0.003);
                float2 fineUV = mistUV * float2(1.5, 1.9) + float2(0.37, 0.18);
                half3 mist = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frac(mistUV + drift)).rgb;
                half3 haze = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frac(fineUV - drift * 1.4)).rgb;
                return half4(lerp(mist, haze, 0.30), opacity);
            }
            ENDHLSL
        }
    }
}
