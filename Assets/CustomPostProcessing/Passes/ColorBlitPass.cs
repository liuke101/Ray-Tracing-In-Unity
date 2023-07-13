using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom Post-processing/Color Blit", typeof(UniversalRenderPipeline))]
public class ColorBlitPass : CustomPostProcessingManager
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    
    private Material m_Material;
    private const string ShaderName = "CustomPostProcessing/ColorBlit";
    
    public override PassInjectionPoint passInjectionPoint => PassInjectionPoint.BeforeRenderingPostProcessing;
    
    public override int orderInPass => 1;
    
    public override bool IsActive() => m_Material != null && intensity.value > 0f;
    
    public override void Setup()
    {
        if (m_Material == null)
        {
            m_Material = CoreUtils.CreateEngineMaterial(ShaderName);
        }
    }
    
    public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination)
    {
        if(m_Material == null)
            return;
        m_Material.SetFloat("_Intensity", intensity.value);
        Blitter.BlitCameraTexture(cmd, source, destination, m_Material,0);
    }
    
    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(m_Material);
    }
}