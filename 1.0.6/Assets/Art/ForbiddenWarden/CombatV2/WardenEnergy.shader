Shader "Arcane Archive/Warden Energy"
{
    Properties
    {
        [HDR] _VeilColor ("Inner violet", Color) = (.38,.06,1.25,1)
        [HDR] _RimColor ("Illuminated edge", Color) = (1.8,.9,.27,1)
        _Strength ("Strength", Float) = 1
        _Clock ("Battle clock", Float) = 0
    }
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
                half4 _VeilColor, _RimColor;
                float _Strength, _Clock;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 f=frac(p);float2 i=floor(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float u=i.uv.x,v=i.uv.y;
                float n=Noise(float2(u*64-_Clock*2,v*9));
                float fine=Noise(float2(u*190+_Clock*3,v*22));
                float veil=smoothstep(0,.10,v)*(1-smoothstep(.87,1,v));
                float striae=pow(saturate(.5+.5*sin(v*48+n*4+u*17-_Clock*2.3)),9);
                float edge=exp(-abs(v-.79-n*.012)*90);
                float feather=saturate((n*.8+fine*.35)-v*.26);
                float body=(.12+feather*.33+striae*.24)*veil;
                half3 color=lerp(_VeilColor.rgb,_RimColor.rgb,smoothstep(.58,.83,v));
                half alpha=saturate((body+edge*.85)*i.color.a*_Strength);
                return half4(color*i.color.rgb*(.8+edge*.65),alpha);
            }
            ENDHLSL
        }
    }
}
