Shader "Dead Signal/Powered Territory"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.01, 0.45, 0.55, 0.22)
        _EdgeColor("Edge Color", Color) = (0.05, 0.95, 1, 0.9)
        _Pulse("Transition Pulse", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Territory"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                half _Pulse;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float radius = saturate(length(input.positionOS.xz) * 2.0);
                float edge = smoothstep(0.76, 1.0, radius);
                float scan = sin((input.positionOS.x + input.positionOS.z) * 34.0 - _Time.y * 2.2) * 0.5 + 0.5;
                float circuitry = smoothstep(0.91, 1.0, scan) * (1.0 - edge) * 0.1;
                float radialFalloff = lerp(0.34, 0.08, radius);
                half4 color = lerp(_BaseColor, _EdgeColor, edge);
                color.a = _BaseColor.a * radialFalloff + edge * _EdgeColor.a * 0.72 + circuitry + _Pulse * edge * 0.28;
                color.rgb += circuitry * _EdgeColor.rgb + _Pulse * _EdgeColor.rgb * edge;
                return color;
            }
            ENDHLSL
        }
    }
}
