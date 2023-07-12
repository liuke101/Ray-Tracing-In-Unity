Shader "RayTracing/AntiAliasing"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 100
        ZWrite Off Cull Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "AntiAliasing"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            float _Sample;
            SAMPLER(sampler_BlitTexture);
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                //以 1 的不透明度绘制第一个样本，接下来是 1/2，然后是 1/3，以此类推，平均所有具有相等贡献的样本。
                float4 color = float4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.texcoord).rgb,1.0f/(_Sample+1.0f));
                return color;
            }
            ENDHLSL
        }
    }
}