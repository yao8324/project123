Shader "Arcane Archive/Temporal Stop"
{
    Properties
    {
        _TemporalPhase ("Age, hold, duration, expansion", Vector) = (0,.45,.75,.18)
        _Aspect ("Camera aspect", Float) = 1.77778
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Grey time wave"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _TemporalPhase;
                float _Aspect;
            CBUFFER_END
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                float2 radial=(uv-.5)*float2(_Aspect,1);
                float distanceFromCentre=length(radial)/length(float2(_Aspect,1)*.5);
                float age=_TemporalPhase.x;
                float entering=smoothstep(0,_TemporalPhase.w,age);
                float leaving=smoothstep(_TemporalPhase.y,_TemporalPhase.z,age);
                float radius=lerp(-.055,1.08,entering)*(1-leaving)-.055*leaving;
                float greyMask=1-smoothstep(radius-.018,radius+.018,distanceFromCentre);
                float edge=exp(-abs(distanceFromCentre-radius)*125);
                float secondEdge=exp(-abs(distanceFromCentre-radius-.032)*185)*.36;
                float waveGate=step(0,radius)*step(radius,1.04);
                edge*=waveGate;secondEdge*=waveGate;
                float2 normal=radial/max(length(radial),.001)/float2(_Aspect,1);
                float2 sampleUv=saturate(uv+normal*edge*.0035);
                half4 original=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,sampleUv,_BlitMipLevel);
                half luminance=dot(original.rgb,half3(.2126,.7152,.0722));
                half silver=luminance*.88h+.018h;
                half3 result=lerp(original.rgb,silver.xxx,greyMask);
                // A narrow silver rim with a dark outer edge reads as a moving boundary,
                // rather than an opaque screen overlay; scene details remain visible.
                result*=1-secondEdge*.25;
                result+=edge*.20+secondEdge*.065;
                return half4(result,original.a);
            }
            ENDHLSL
        }
    }
}
