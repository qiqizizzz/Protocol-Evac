Shader "Protocol_Evac/FX/LootInteractableOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.68, 0.18, 0.65)
        _OutlineWidth ("Outline Width", Range(0, 0.08)) = 0.014
        _PulseSpeed ("Pulse Speed", Float) = 2.4
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LootOutline"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth * pulse;
                output.positionCS = TransformObjectToHClip(positionOS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(_OutlineColor.rgb, _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
