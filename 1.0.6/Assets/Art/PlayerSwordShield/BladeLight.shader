Shader "Arcane Archive/Blade Light"
{
    Properties { [HDR] _BaseColor ("Light", Color) = (1.6,1.05,.42,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.color=v.color; o.uv=v.uv;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half across = abs(i.uv.y * 2 - 1);
                half veil = pow(saturate(1 - across), 1.8h);
                half filament = pow(saturate(1 - across), 18.0h);
                // Geometry and vertex age own the fade, including pause. No autonomous _Time animation.
                half3 tint = _BaseColor.rgb * i.color.rgb;
                half3 lit = tint * (0.7h + 0.65h * filament);
                return half4(lit, _BaseColor.a * i.color.a * veil);
            }
            ENDHLSL
        }
    }
}
