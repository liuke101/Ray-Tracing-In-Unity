using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


/// <summary>
/// 不同注入点的RenderPass
/// </summary>
public class CustomPostProcessingPass : ScriptableRenderPass
{
    //所有CustomPostProcessing后处理基类实例 
    private List<CustomPostProcessingManager> CustomPostProcessingList;

    //当前active组件下标
    private List<int> ActiveCustomPostProcessingIndexList;

    //每个组件对应的ProfilingSampler
    private string m_ProfilerTag;
    private List<ProfilingSampler> ProfilingSamplersList;

    //声明RT
    private RTHandle m_CameraColorRT;
    private RTHandle m_TempRT0;
    private RTHandle m_TempRT1;

    //临时RT名称
    private string tempRT0Name => "_TemporaryRenderTexture0";
    private string tempRT1Name => "_TemporaryRenderTexture1";

    //RT描述符
    private RenderTextureDescriptor RTDescriptor;

    /// <summary>
    /// 构造函数,向其传递当前注入点的所有 CustomPostProcessing 实例
    /// </summary>
    /// <param name="profilerTag"></param>
    /// <param name="customPostProcessingList"></param>
    public CustomPostProcessingPass(string profilerTag, List<CustomPostProcessingManager> customPostProcessingList)
    {
        m_ProfilerTag = profilerTag;
        CustomPostProcessingList = customPostProcessingList;
        ActiveCustomPostProcessingIndexList = new List<int>(customPostProcessingList.Count);

        //将自定义后处理器对象列表转化成一个性能采样器对象列表
        ProfilingSamplersList = customPostProcessingList.Select(c => new ProfilingSampler(c.ToString())).ToList();
    }

    /// <summary>
    /// 获取active的CustomPostProcessing的在列表中的下标
    /// </summary>
    /// <returns>是否存在有效组件</returns>
    public bool SetupCustomPostProcessing()
    {
        ActiveCustomPostProcessingIndexList.Clear();
        for (int i = 0; i < CustomPostProcessingList.Count; i++)
        {
            CustomPostProcessingList[i].Setup();
            if (CustomPostProcessingList[i].IsActive())
            {
                //Debug.Log(CustomPostProcessingList[i]+"已激活");
                ActiveCustomPostProcessingIndexList.Add(i);
            }
        }

        return ActiveCustomPostProcessingIndexList.Count != 0;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        //获取RTDescriptor，描述RT的信息
        RTDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RTDescriptor.msaaSamples = 1;
        RTDescriptor.depthBufferBits = 0; //Color and depth cannot be combined in RTHandles

        //设置相机RT
        m_CameraColorRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
    }

    /// <summary>
    /// 渲染逻辑
    /// </summary>
    /// <param name="context"></param>
    /// <param name="renderingData"></param>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //初始化commandbuffer
        var cmd = CommandBufferPool.Get(m_ProfilerTag);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        //创建TempRT0临时RT
        RenderingUtils.ReAllocateIfNeeded(ref m_TempRT0, RTDescriptor, name: tempRT0Name);

        //标记是否使用过TempRT1
        bool rt1Used = false;

        //执行组件的Render方法
        //如果只有一个后处理效果（组件），则直接将这个后处理效果从 mSourceRT 渲染到mTempRT0
        if (ActiveCustomPostProcessingIndexList.Count == 1)
        {
            int index = ActiveCustomPostProcessingIndexList[0];
            using (new ProfilingScope(cmd, ProfilingSamplersList[index]))
            {
                CustomPostProcessingList[index].Render(cmd, ref renderingData, m_CameraColorRT, m_TempRT0);
            }
        }
        //如果有多个后处理效果（组件），则在两个RT上来回bilt。
        //每个组件执行Render函数将mTempRT0绘制到mTempRT1上，然后在循环结束时交换mTempRT0和mTempRT1，最终纹理依然存在mTempRT0。
        //如此循环往复。最后所有的后处理效果都渲染到mTempRT0上。
        else
        {
            //声明TempRT1临时纹理
            RenderingUtils.ReAllocateIfNeeded(ref m_TempRT1, RTDescriptor, name: tempRT1Name);
            rt1Used = true;

            //将相机RT blit到TempRT0
            Blit(cmd, m_CameraColorRT, m_TempRT0);
            //Blitter.BlitCameraTexture(cmd,m_CameraColorRT,m_TempRT0);

            for (int i = 0; i < ActiveCustomPostProcessingIndexList.Count; i++)
            {
                int index = ActiveCustomPostProcessingIndexList[i];
                CustomPostProcessingManager customPostProcessingManager = CustomPostProcessingList[index];
                using (new ProfilingScope(cmd, ProfilingSamplersList[index]))
                {
                    //在renderpass中将mTempRT0 blit到TempRT1
                    customPostProcessingManager.Render(cmd, ref renderingData, m_TempRT0, m_TempRT1);
                }

                //交换mTempRT0和TempRT1
                CoreUtils.Swap(ref m_TempRT0, ref m_TempRT1);
                //Blitter.BlitCameraTexture(cmd,m_TempRT1,m_TempRT0);
            }
        }

        //将m_TempRT0 blit到相机RT
        Blit(cmd, m_TempRT0, m_CameraColorRT);
        //Blitter.BlitCameraTexture(cmd, m_TempRT0, m_CameraColorRT);

        //释放临时RT
        cmd.ReleaseTemporaryRT(Shader.PropertyToID(tempRT0Name));
        if (rt1Used)
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(tempRT1Name));

        //释放临时RT(黑屏？)
        //m_TempRT0?.Release();
        //m_TempRT1?.Release();
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}