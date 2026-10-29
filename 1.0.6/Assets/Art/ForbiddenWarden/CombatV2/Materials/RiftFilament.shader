Shader "Arcane Archive/Rift Filament"
{
    Properties
    {
        [HDR] _Tint ("Violet edge", Color) = (0.28,0.015,0.48,1)
        [HDR] _Core ("Crimson core", Color) = (2.1,0.12,0.32,1)
        _Strength ("Energy", Float) = 1
        _Reveal ("Branch growth", Range(0,1)) = 1
        _Fade ("Fade", Range(0,1)) = 1
        _Phase ("Battle clock", Float) = 0
        _Mode ("0 filament, 1 halo, 2 plume, 3 scar", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Rift"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint, _Core;
                float _Strength, _Reveal, _Fade, _Phase, _Mode, _DstBlend;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=v.uv;o.color=v.color;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float edge=saturate(1-abs(i.uv.y*2-1));
                float growth=1-smoothstep(_Reveal-.12,_Reveal+.02,i.uv.x);
                float flow=.82+.18*sin(i.uv.x*28-_Phase*7+i.color.r*9);
                float core=pow(edge,8);
                half3 tint=lerp(_Tint.rgb,_Core.rgb,core);
                float alpha=pow(edge,1.6)*growth*_Fade*i.color.a;
                if(_Mode>.5 && _Mode<1.5)
                {
                    float radius=i.uv.x;
                    alpha=pow(saturate(1-radius),2.8)*_Fade*.18;
                    tint=lerp(_Core.rgb,_Tint.rgb,smoothstep(.1,.8,radius));
                    flow=.9+.1*sin(radius*22-_Phase*5);
                }
                else if(_Mode>1.5 && _Mode<2.5)
                {alpha*=sin(saturate(i.uv.x)*3.14159265);tint=lerp(_Core.rgb,_Tint.rgb,i.uv.x);}
                else if(_Mode>2.5)
                {return half4(.012,.008,.018,edge*_Fade*.85*i.color.a);}
                return half4(tint*_Strength*flow,alpha);
            }
            ENDHLSL
        }
    }
}
