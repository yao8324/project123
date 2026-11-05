Shader "Arcane Archive/Spell Mote"
{
    Properties { [HDR] _BaseColor ("Radiance", Color) = (3,3,3,1) }
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
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.color=v.color;o.uv=v.uv;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half2 p=abs(i.uv*2-1);
                half halo=pow(saturate(1-length(p)),3);
                half core=pow(saturate(1-length(p)*3.4h),2);
                half star=pow(saturate(1-min(p.x,p.y)*14),3)*pow(saturate(1-max(p.x,p.y)),2);
                half alpha=saturate(halo*.42h+core+star*.65h);
                return half4(_BaseColor.rgb*i.color.rgb,_BaseColor.a*i.color.a*alpha);
            }
            ENDHLSL
        }
    }
}
