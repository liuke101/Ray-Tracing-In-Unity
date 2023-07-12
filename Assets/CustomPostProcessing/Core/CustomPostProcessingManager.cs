using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 后处理Pass插入点
/// </summary>
public enum PassInjectionPoint
{
    BeforeRenderingPrePasses,
    AfterRenderingPrePasses,
    BeforeRenderingGbuffer,
    AfterRenderingGbuffer,
    BeforeRenderingDeferredLights,
    AfterRenderingDeferredLights,
    BeforeRenderingOpaques,
    AfterRenderingOpaques,
    BeforeRenderingSkybox,
    AfterRenderingSkybox,
    BeforeRenderingTransparents,
    AfterRenderingTransparents,
    BeforeRenderingPostProcessing,
    AfterRenderingPostProcessing,
    AfterRendering,
}

/// <summary>
/// 后处理基类
/// 自定义后处理Pass继承这个基类，在Execute函数中重载Render函数实现自己的渲染逻辑
/// </summary>
public abstract class CustomPostProcessingManager : VolumeComponent,IPostProcessComponent,IDisposable
{
    //插入点
    public virtual PassInjectionPoint passInjectionPoint => PassInjectionPoint.AfterRenderingPostProcessing;
    
    //在插入的Pass中的顺序
    public virtual int orderInPass => 0;
    
    public abstract bool IsActive();
    
    /// <summary>
    /// 配置当前后处理
    /// </summary>
    public abstract void Setup();
    
    /// <summary>
    /// 执行渲染
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="renderingData"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    public abstract void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination); 

    public virtual bool IsTileCompatible() => false; 

    /// <summary>
    /// 清理临时RenderTexture
    /// </summary>
    public void Dispose() {  
        Dispose(true);  
        GC.SuppressFinalize(this);  
    }  
    
    //Override this function to clean up resources in your renderer
    public virtual void Dispose(bool disposing) { }  
    
}
