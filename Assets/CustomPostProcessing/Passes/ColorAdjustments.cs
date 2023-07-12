using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom Post-processing/ColorAdjustments", typeof(UniversalRenderPipeline))]
public class ColorAdjustments : CustomPostProcessingManager
{
    #region 变量定义
    //后曝光
    public FloatParameter postExposure = new FloatParameter(0.0f);

    //对比度
    public ClampedFloatParameter contrast = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);

    //颜色滤镜
    public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);

    //色相偏移
    public ClampedFloatParameter hueShift = new ClampedFloatParameter(0.0f, -180.0f, 180.0f);

    //饱和度
    public ClampedFloatParameter saturation = new ClampedFloatParameter(0.0f, -100.0f, 100.0f);
    #endregion
    
    //blit材质
    private Material m_Material;
    private const string ShaderName = "CustomPostProcessing/ColorAdjustments";

    public override PassInjectionPoint passInjectionPoint => PassInjectionPoint.BeforeRenderingPostProcessing;
    public override int orderInPass => 0;

    private int m_ColorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    private int m_ColorFilterId = Shader.PropertyToID("_ColorFilter");
    private const string ExposureKeyword = "EXPOSURE";
    private const string ContrastKeyword = "CONTRAST";
    private const string HueShiftKeyword = "HUE_SHIFT";
    private const string SaturationKeyword = "SATURATION";
    private const string ColorFilterKeyword = "COLOR_FILTER";

    public override bool IsActive() => m_Material != null && (postExposure.value != 0.0f || contrast.value != 0.0f || colorFilter.value != Color.white || hueShift.value != 0.0f || saturation.value != 0.0f);
    
    public override void Setup()
    {
        if (m_Material == null)
            m_Material = CoreUtils.CreateEngineMaterial(ShaderName);
    }

    public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source,
        RTHandle destination)
    {
        if (m_Material == null) 
            return;
        
        Vector4 colorAdjustmentsVector4 = new Vector4(
            Mathf.Pow(2f, postExposure.value), // 曝光度 曝光单位是2的幂次
            contrast.value * 0.01f + 1f, // 对比度 将范围从[-100, 100]映射到[0, 2]
            hueShift.value * (1.0f / 360.0f), // 色相偏移 将范围从[-180, 180]转换到[-0.5, 0.5]
            saturation.value * 0.01f + 1.0f); // 饱和度 将范围从[-100, 100]转换到[0, 2]
        m_Material.SetVector(m_ColorAdjustmentsId, colorAdjustmentsVector4);
        m_Material.SetColor(m_ColorFilterId, colorFilter.value);

        //设置keyword
        SetKeyWord(ExposureKeyword, postExposure.value != 0.0f);
        SetKeyWord(ContrastKeyword, contrast.value != 0.0f);
        SetKeyWord(HueShiftKeyword, hueShift.value != 0.0f);
        SetKeyWord(SaturationKeyword, saturation.value != 0.0f);
        SetKeyWord(ColorFilterKeyword, colorFilter.value != Color.white);
        
        //将src RTHandle blit到dest RTHandle
        Blitter.BlitCameraTexture(cmd, source, destination, m_Material,0);
    }

    private void SetKeyWord(string keyword, bool enabled = true)
    {
        if (enabled)
            m_Material.EnableKeyword(keyword);
        else
            m_Material.DisableKeyword(keyword);
    }

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(m_Material);
    }
}