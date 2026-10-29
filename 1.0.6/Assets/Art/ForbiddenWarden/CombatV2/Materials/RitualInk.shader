Shader "Arcane Archive/Ritual Ink"
{
    Properties
    {
        [HDR] _Tint ("Ink and radiance", Color) = (1,0.6,0.2,1)
        _Fade ("Battle-clock fade", Range(0,1)) = 1
        _Phase ("Battle clock", Float) = 0
        _Soft ("Soft ribbon", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 vertex:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 position:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Fade, _Phase, _Soft, _DstBlend;
            CBUFFER_END
            Varyings Vert(Attributes v)
            { Varyings o; o.position=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv;o.color=v.color;return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float edge=pow(saturate(1-abs(i.uv.y*2-1)),2);
                float shape=lerp(1,edge,saturate(_Soft));
                float flow=.88+.12*sin(i.uv.x*29-_Phase*8);
                return half4(_Tint.rgb*i.color.rgb*flow,_Tint.a*i.color.a*_Fade*shape);
            }
            ENDHLSL
        }
    }
}
