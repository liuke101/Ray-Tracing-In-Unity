Shader "Custom/RayTracingRT"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "RayTracingRT"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            //声明采样器，_BlitTexture纹理已经在Blit.hlsl中声明
            //或者直接使用公共采样器sampler_LinearClamp等等
            SAMPLER(sampler_BlitTexture);
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                //float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.texcoord);
                float4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, input.texcoord);
                return color;
            }
            ENDHLSL
        }
    }
}