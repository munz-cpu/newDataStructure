Shader "Divine War/Hover Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 1
        _FillOpacity ("Fill Opacity", Range(0, 1)) = 0
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
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                half _FillOpacity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 position = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(position);
                output.uv = input.uv;
                return output;
            }

            half Alpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 stepX = float2(_MainTex_TexelSize.x * _OutlineWidth, 0);
                float2 stepY = float2(0, _MainTex_TexelSize.y * _OutlineWidth);
                half neighbor = max(max(Alpha(uv + stepX), Alpha(uv - stepX)),
                    max(Alpha(uv + stepY), Alpha(uv - stepY)));
                neighbor = max(neighbor, max(Alpha(uv + stepX + stepY), Alpha(uv - stepX - stepY)));
                neighbor = max(neighbor, max(Alpha(uv + stepX - stepY), Alpha(uv - stepX + stepY)));
                half center = Alpha(uv);
                half opacity = saturate(neighbor - center) + center * _FillOpacity;
                return half4(_OutlineColor.rgb, _OutlineColor.a * opacity);
            }
            ENDHLSL
        }
    }
}
