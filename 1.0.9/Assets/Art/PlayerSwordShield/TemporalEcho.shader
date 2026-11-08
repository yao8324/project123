Shader "Arcane Archive/Temporal Echo"
{
    Properties
    {
        [HDR] _Tint ("Spectral tint", Color) = (.15,1,.7,1)
        _Opacity ("Opacity", Range(0,1)) = .4
        _Clock ("Local clock", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Opacity;
                float _Clock;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half rim = pow(1 - saturate(abs(dot(normalize(i.normalWS), GetWorldSpaceNormalizeViewDir(i.positionWS)))), 2);
                half scan = .75h + .25h * sin(i.positionWS.y * 135 + _Clock * 22);
                half filament = pow(saturate(sin(i.positionWS.y * 37 - _Clock * 7)), 12);
                return half4(_Tint.rgb * (1 + rim * 1.2h + filament * .3h), _Opacity * (.16h + rim * .84h) * scan);
            }
            ENDHLSL
        }
    }
}
