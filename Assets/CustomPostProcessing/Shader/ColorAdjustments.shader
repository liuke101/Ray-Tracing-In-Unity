Shader "CustomPostProcessing/ColorAdjustments"
{
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
            Name "ColorAdjusmentsPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            #pragma shader_feature CONTRAST
            #pragma shader_feature COLOR_FILTER
            #pragma shader_feature HUE_SHIFT
            #pragma shader_feature SATURATION
            #pragma shader_feature INTENSITY;
            
            //曝光度，对比度，色相便宜，饱和度
            float4 _ColorAdjustments;
            float4 _ColorFilter;

            //声明采样器，_BlitTexture纹理已经在Blit.hlsl中声明
            //或者直接使用公共采样器sampler_LinearClamp等等
            SAMPLER(sampler_BlitTexture);
           
            
            //曝光度
            float3 ColorAdjustmentExposure(float3 color)
            {
                return color * _ColorAdjustments.x;
            }

            //对比度
            float3 ColorAdjustmentContrast(float3 color)
            {
                // 为了更好的效果 将颜色从线性空间转换到logC空间（因为要取美术中灰）
                color = LinearToLogC(color);
                //从颜色中减去均匀的中间灰度，然后通过对比度进行缩放，然后在中间添加灰度
                color = (color - ACEScc_MIDGRAY)*_ColorAdjustments.y+ ACEScc_MIDGRAY;
                return LogCToLinear(color);
            }

            //颜色滤镜
            float3 ColorAdjustmentColorFilter(float3 color)
            {
                color = SRGBToLinear(color);
                color = color * _ColorFilter.rgb;
                return color;
            }

            //色相偏移
            float3 ColorAdjustmentHueShift(float3 color)
            {
                //将颜色格式从rgb转换为hsv
                color = RgbToHsv(color);//将色相偏移添加到h
                float hue = color.x + _ColorAdjustments.z;
                //如果色相超出范围，将其截断
                color.x = RotateHue(hue,0.0,1.0);
                //将颜色格式从hsv转换为rgb
                return HsvToRgb(color);
            }

            //饱和度
            float3 ColorAdjustmentSaturation(float3 color)
            {
                //获取颜色的亮度
                float luminance = Luminance(color);
                //从颜色中减去亮度，然后通过饱和度进行缩放，然后在中间添加亮度
                return (color-luminance)*_ColorAdjustments.w+luminance;
            }

            float3 ColorAdjustment(float3 color)
            {
                //防止颜色值过大的潜在隐患
                color = min(color,60.0);
                
                //曝光度
                #ifdef EXPOSURE
                color = ColorAdjustmentExposure(color);
                #endif
                
                //对比度
                #ifdef CONTRAST
                color = ColorAdjustmentContrast(color);
                #endif
                //颜色滤镜
                #ifdef COLOR_FILTER
                color = ColorAdjustmentColorFilter(color);
                #endif
                //色相偏移
                #ifdef HUE_SHIFT
                color = ColorAdjustmentHueShift(color);
                #endif
                
                //饱和度
                #ifdef SATURATION
                color = ColorAdjustmentSaturation(color);
                #endif
                
                //当饱和度增加时，可能产生负数，在这之后将颜色限制到0
                return max(color,0.0);
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.texcoord);
                return color *float4(ColorAdjustment(color),1);
            }
            ENDHLSL
        }
    }
}