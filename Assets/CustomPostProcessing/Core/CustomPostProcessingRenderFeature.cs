using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 自定义Render Feature 类
/// 获取所有 Volume 中 CustomPostProcessing 的子类（），并根据它们的具体设置（如注入位置等）创建对应的自定义 RenderPass 实例
/// </summary>
public class CustomPostProcessingRenderFeature : ScriptableRendererFeature
{
    // 不同插入点的render pass
    private CustomPostProcessingPass m_BeforeRenderingPrePasses;
    private CustomPostProcessingPass m_AfterRenderingPrePasses;
    private CustomPostProcessingPass m_BeforeRenderingGbuffer;
    private CustomPostProcessingPass m_AfterRenderingGbuffer;
    private CustomPostProcessingPass m_BeforeRenderingDeferredLights;
    private CustomPostProcessingPass m_AfterRenderingDeferredLights;
    private CustomPostProcessingPass m_BeforeRenderingOpaques;
    private CustomPostProcessingPass m_AfterRenderingOpaques;
    private CustomPostProcessingPass m_BeforeRenderingSkybox;
    private CustomPostProcessingPass m_AfterRenderingSkybox;
    private CustomPostProcessingPass m_BeforeRenderingTransparents;
    private CustomPostProcessingPass m_AfterRenderingTransparents;
    private CustomPostProcessingPass m_BeforeRenderingPostProcessing;
    private CustomPostProcessingPass m_AfterRenderingPostProcessing;
    private CustomPostProcessingPass m_AfterRendering;
    
    // 所有后处理基类列表
    private List<CustomPostProcessingManager> CustomPostProcessingList;

    // 获取所有的CustomPostProcessing实例，并且根据插入点排序，放入到对应Render Pass中
    public override void Create()
    {
        //获取VolumeStack
        var stack = VolumeManager.instance.stack;

        //-------------------------------------------
        //获取volumeStack中所有CustomPostProcessing实例
        //-------------------------------------------
        
        //1.获取所有继承自VolumeComponent的派生类类型
        var derivedVolumeComponentTypes = VolumeManager.instance.baseComponentTypeArray;
        //2.筛选出派生类型为 CustomPostProcessing 的实例列表
        CustomPostProcessingList = derivedVolumeComponentTypes
            .Where(t => t.IsSubclassOf(typeof(CustomPostProcessingManager))) //// 筛选出volumeStack中的CustomPostProcessing类型元素
            .Select(t => stack.GetComponent(t) as CustomPostProcessingManager) //将类型元素转换为实例 
            .ToList(); // 转换为List
        
        //-------------------------------------------
        //初始化不同插入点的render pass
        //对抓取到的所有 CustomPostProcessing 实例按照注入点分类，并且 按照在注入点的顺序进行排序
        //-------------------------------------------
        
        #region  不同插入点的render pass
       
        //BeforeRenderingPrePasses
        //找到注入点为BeforeRenderingPrePasses的CustomPostProcessing
        var BeforeRenderingPrePassesCPPs = CustomPostProcessingList.
            //筛选出所有CustomPostProcessing类中注入点为BeforeRenderingPrePasses的实例
            Where(c => c.passInjectionPoint == PassInjectionPoint.BeforeRenderingPrePasses)
            .OrderBy(c => c.orderInPass) //按照顺序排序
            .ToList(); //转换为List
        //创建CustomPostProcessingPass类
        m_BeforeRenderingPrePasses = new CustomPostProcessingPass("Custom PostProcess Before Rendering Pre Passes", BeforeRenderingPrePassesCPPs);
        //设置Pass执行时间
        m_BeforeRenderingPrePasses.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
        
        //AfterRenderingPrePasses
        var AfterRenderingPrePassesCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRenderingPrePasses).OrderBy(c => c.orderInPass).ToList();
        m_AfterRenderingPrePasses = new CustomPostProcessingPass("Custom PostProcess After Rendering Pre Passes", AfterRenderingPrePassesCPPs);
        m_AfterRenderingPrePasses.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        
        //BeforeRenderingGbuffer
        var BeforeRenderingGbufferCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.BeforeRenderingGbuffer).OrderBy(c => c.orderInPass).ToList();
        m_BeforeRenderingGbuffer = new CustomPostProcessingPass("Custom PostProcess Before Rendering Gbuffer", BeforeRenderingGbufferCPPs);
        m_BeforeRenderingGbuffer.renderPassEvent = RenderPassEvent.BeforeRenderingGbuffer;
        
        //AfterRenderingGbuffer
        var AfterRenderingGbufferCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRenderingGbuffer).OrderBy(c => c.orderInPass).ToList();
        m_AfterRenderingGbuffer = new CustomPostProcessingPass("Custom PostProcess After Rendering Gbuffer", AfterRenderingGbufferCPPs);
        m_AfterRenderingGbuffer.renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
        
        //BeforeRenderingDeferredLights
        var BeforeRenderingDeferredLightsCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.BeforeRenderingDeferredLights).OrderBy(c => c.orderInPass).ToList();
        m_BeforeRenderingDeferredLights = new CustomPostProcessingPass("Custom PostProcess Before Rendering Deferred Lights", BeforeRenderingDeferredLightsCPPs);
        m_BeforeRenderingDeferredLights.renderPassEvent = RenderPassEvent.BeforeRenderingDeferredLights;
        
        //AfterRenderingDeferredLights
        var AfterRenderingDeferredLightsCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRenderingDeferredLights).OrderBy(c => c.orderInPass).ToList();
        m_AfterRenderingDeferredLights = new CustomPostProcessingPass("Custom PostProcess After Rendering Deferred Lights", AfterRenderingDeferredLightsCPPs);
        m_AfterRenderingDeferredLights.renderPassEvent = RenderPassEvent.AfterRenderingDeferredLights;
        
        //BeforeRenderingOpaques
        var BeforeRenderingOpaquesCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.BeforeRenderingOpaques).OrderBy(c => c.orderInPass).ToList();
        m_BeforeRenderingOpaques = new CustomPostProcessingPass("Custom PostProcess Before Rendering Opaques", BeforeRenderingOpaquesCPPs);
        m_BeforeRenderingOpaques.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        
        //AfterRenderingOpaques
        var AfterRenderingOpaquesCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRenderingOpaques).OrderBy(c => c.orderInPass).ToList();
        m_AfterRenderingOpaques = new CustomPostProcessingPass("Custom PostProcess After Rendering Opaques", AfterRenderingOpaquesCPPs);
        m_AfterRenderingOpaques.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        
        //BeforeRenderingSkybox
        var BeforeRenderingSkyboxCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.BeforeRenderingSkybox).OrderBy(c => c.orderInPass).ToList();
        m_BeforeRenderingSkybox = new CustomPostProcessingPass("Custom PostProcess Before Rendering Skybox", BeforeRenderingSkyboxCPPs);
        m_BeforeRenderingSkybox.renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;
        
        //AfterRenderingSkybox
        var AfterRenderingSkyboxCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRenderingSkybox).OrderBy(c => c.orderInPass).ToList();
        m_AfterRenderingSkybox = new CustomPostProcessingPass("Custom PostProcess After Rendering Skybox", AfterRenderingSkyboxCPPs);
        m_AfterRenderingSkybox.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        
        //BeforeRenderingTransparents
        var BeforeRenderingTransparentsCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.BeforeRenderingTransparents).OrderBy(c => c.orderInPass).ToList();
        m_BeforeRenderingTransparents = new CustomPostProcessingPass("Custom PostProcess Before Rendering Transparents", BeforeRenderingTransparentsCPPs);
        m_BeforeRenderingTransparents.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        
        //AfterRenderingTransparents
        var AfterRenderingTransparentsCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRenderingTransparents).OrderBy(c => c.orderInPass).ToList();
        m_AfterRenderingTransparents = new CustomPostProcessingPass("Custom PostProcess After Rendering Transparents", AfterRenderingTransparentsCPPs);
        m_AfterRenderingTransparents.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        //BeforeRenderingPostProcessing
        var BeforeRenderingPostProcessingCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.BeforeRenderingPostProcessing).OrderBy(c => c.orderInPass).ToList();
        m_BeforeRenderingPostProcessing = new CustomPostProcessingPass("Custom PostProcess Before Rendering Post Processing", BeforeRenderingPostProcessingCPPs);
        m_BeforeRenderingPostProcessing.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        //AfterRenderingPostProcessing
        var AfterRenderingPostProcessingCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRenderingPostProcessing).OrderBy(c => c.orderInPass).ToList();
        m_AfterRenderingPostProcessing = new CustomPostProcessingPass("Custom PostProcess After Rendering Post Processing", AfterRenderingPostProcessingCPPs);
        m_AfterRenderingPostProcessing.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        
        //AfterRendering
        var AfterRenderingCPPs = CustomPostProcessingList.Where(c => c.passInjectionPoint == PassInjectionPoint.AfterRendering).OrderBy(c => c.orderInPass).ToList();
        m_AfterRendering = new CustomPostProcessingPass("Custom PostProcess After Rendering", AfterRenderingCPPs);
        m_AfterRendering.renderPassEvent = RenderPassEvent.AfterRendering;
        
        #endregion
    }

    // 将不同注入点的 RenderPass 注入到renderer中
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //当前渲染的相机需要开启后处理
        if (renderingData.cameraData.postProcessEnabled)
        {
            //为每个RenderPass设置RT
            //将 render pass 添加到渲染队列
            #region 所有RenderPass
            
            if(m_BeforeRenderingPrePasses.SetupCustomPostProcessing())
            {
                //ConfigureInput方法提供信息来自动确定是否需要通过中间纹理进行渲染（RendererData的中间纹理设置为Auto）
                m_BeforeRenderingPrePasses.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforeRenderingPrePasses);
            }
            
            if(m_AfterRenderingPrePasses.SetupCustomPostProcessing())
            {
                m_AfterRenderingPrePasses.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRenderingPrePasses);
            }
            
            if(m_BeforeRenderingGbuffer.SetupCustomPostProcessing())
            {
                m_BeforeRenderingGbuffer.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforeRenderingGbuffer);
            }
            
            if(m_AfterRenderingGbuffer.SetupCustomPostProcessing())
            {
                m_AfterRenderingGbuffer.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRenderingGbuffer);
            }
            
            if(m_BeforeRenderingDeferredLights.SetupCustomPostProcessing())
            {
                m_BeforeRenderingDeferredLights.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforeRenderingDeferredLights);
            }
            
            if(m_AfterRenderingDeferredLights.SetupCustomPostProcessing())
            {
                m_AfterRenderingDeferredLights.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRenderingDeferredLights);
            }
            
            if(m_BeforeRenderingOpaques.SetupCustomPostProcessing())
            {
                m_BeforeRenderingOpaques.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforeRenderingOpaques);
            }
            
            if(m_AfterRenderingOpaques.SetupCustomPostProcessing())
            {
                m_AfterRenderingOpaques.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRenderingOpaques);
            }
            
            if(m_BeforeRenderingSkybox.SetupCustomPostProcessing())
            {
                m_BeforeRenderingSkybox.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforeRenderingSkybox);
            }
            
            if(m_AfterRenderingSkybox.SetupCustomPostProcessing())
            {
                m_AfterRenderingSkybox.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRenderingSkybox);
            }
            
            if(m_BeforeRenderingTransparents.SetupCustomPostProcessing())
            {
                m_BeforeRenderingTransparents.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforeRenderingTransparents);
            }
            
            if(m_AfterRenderingTransparents.SetupCustomPostProcessing())
            {
                m_AfterRenderingTransparents.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRenderingTransparents);
            }
            
            if(m_BeforeRenderingPostProcessing.SetupCustomPostProcessing())
            {
                m_BeforeRenderingPostProcessing.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforeRenderingPostProcessing);
            }
            
            if(m_AfterRenderingPostProcessing.SetupCustomPostProcessing())
            {
                m_AfterRenderingPostProcessing.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRenderingPostProcessing);
            }
            
            if(m_AfterRendering.SetupCustomPostProcessing())
            {
                m_AfterRendering.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterRendering);
            }
            #endregion
        }
    }

    //释放抓取的 CustomPostProcessing 实例
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && CustomPostProcessingList != null)
        {
            foreach (var item in CustomPostProcessingList)
            {
                item.Dispose();
            }
        }
    }

}