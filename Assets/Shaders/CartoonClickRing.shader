Shader "Protocol_Evac/FX/CartoonClickRing"
{
    Properties
    {
        _BlackColor ("Black Color",Color) = (0.03,0.03,0.03,1)
        _WhiteColor ("White Color",Color) = (0.95,0.95,0.95,1)
        
        _StartTime("Start Time",Float) = 0
        _Duration("Duration", Range(0.1,3.0)) = 0.6
        
        _StartRadius("Start Radius", Range(0.01, 1.0)) = 0.12
        _EndRadius("End Radius", Range(0.01, 2.0)) = 0.75
        
        _OutlineWidth("Outline Width", Range(0.001, 0.2)) = 0.065
        _HighlightWidth("Highlight Width", Range(0.001, 0.2)) = 0.025
        _EdgeSoftness("Edge Softness", Range(0.001, 0.1)) = 0.01
        
        _EchoOffset("Echo Offset", Range(0.0, 0.5)) = 0.12
        _EchoWidth("Echo Width", Range(0.001, 0.2)) = 0.025
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BlackColor;
                float4 _WhiteColor;
            
                float _StartTime;
                float _Duration;
            
                float _StartRadius;
                float _EndRadius;
            
                float _OutlineWidth;
                float _HighlightWidth;
                float _EdgeSoftness;
            
                float _EchoOffset;
                float _EchoWidth;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
        }

            half4 Frag(Varyings input) : SV_Target
            {
                float elapsedTime = _Time.y - _StartTime;
                float progress = saturate(elapsedTime / max(_Duration, 0.0001));

                float easedProgress = 1.0 - pow(1.0 - progress, 3.0);

                float radius = lerp(_StartRadius, _EndRadius, easedProgress);

                float2 centeredUV = input.uv * 2.0 - 1.0;

                float distanceToCenter = length(centeredUV);

                float outlineDistance = abs(distanceToCenter - radius);

                float outline = 1.0 - smoothstep(_OutlineWidth,_OutlineWidth+_EdgeSoftness,outlineDistance);

                float highlightRadius = radius * 0.96;

                float highlightDistance = abs(distanceToCenter - highlightRadius);

                float highlight = 1.0 - smoothstep(_HighlightWidth, _HighlightWidth + _EdgeSoftness, highlightDistance);

                float echoRadius = radius + _EchoOffset;

                float echoDistance = abs(distanceToCenter - echoRadius);

                float echo = 1.0 - smoothstep(_EchoWidth, _EchoWidth + _EdgeSoftness, echoDistance);

                float fade = 1.0 - smoothstep(0.65, 1.0, progress);

                float blackAlpha = outline * fade;

                float whiteAlpha = saturate(highlight + echo * 0.3) * fade;

                float3 finalColor = lerp(_BlackColor.rgb, _WhiteColor.rgb, whiteAlpha);

                float finalAlpha = max(blackAlpha, whiteAlpha);

                return half4(finalColor, finalAlpha);
            }
        ENDHLSL
        }
    }
}
