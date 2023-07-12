using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

[Serializable,
 VolumeComponentMenuForRenderPipeline("Custom Post-processing/Ray Tracing", typeof(UniversalRenderPipeline))]
public class RayTracingPass : CustomPostProcessingManager
{
    //ComputeShader相关
    public VolumeParameter<ComputeShader> computeShader = new VolumeParameter<ComputeShader>();
    private Material m_Material;
    private int m_MainTextureID = Shader.PropertyToID("_MainTex");
    //private static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
    private RenderTexture m_RT;
    private int m_KernelIndex;
    
    //天空盒
    public TextureParameter SkyboxTexture = new TextureParameter(null);

    //抗锯齿
    private uint m_CurrentSample = 0;
    private Material m_AntiAliasingMaterial;
    private int m_Sample = Shader.PropertyToID("_Sample");
    
    //相机
    private Camera m_Camera;
    

    //Pass插入点
    public override PassInjectionPoint passInjectionPoint => PassInjectionPoint.BeforeRenderingPostProcessing;

    //在Pass内的排序
    public override int orderInPass => 2;

    //激活状态
    public override bool IsActive() => computeShader != null&& m_Material != null;

    //配置
    public override void Setup()
    {
        //主相机
        if(m_Camera == null)
            m_Camera = Camera.main;
        
        if (m_Material == null)
        {
            m_Material = CoreUtils.CreateEngineMaterial("RayTracing/RayTracingRT");
        }
        
        if (m_RT == null || m_RT.width != Screen.width || m_RT.height != Screen.height)
        {
            if (m_RT != null)
                m_RT.Release();

            //创建一张和屏幕大小一样的RT
            m_RT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            //RWTexture为UAV类型，支持无序(随机）访问读写操作
            m_RT.enableRandomWrite = true;
            m_RT.Create();
        }
        
        if(m_AntiAliasingMaterial== null)
            m_AntiAliasingMaterial = CoreUtils.CreateEngineMaterial("RayTracing/AntiAliasing");
    }

    //渲染
    public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source,
        RTHandle destination)
    {
        if (computeShader.value == null || m_RT == null || m_Material == null || m_AntiAliasingMaterial == null)
            return;

        //检测相机变换
        if (m_Camera.transform.hasChanged)
        {
            m_CurrentSample = 0;
            m_Camera.transform.hasChanged = false;
        }
        
        //设置shader参数
        SetShaderParameters(cmd, ref renderingData, source, destination);
        
        //核函数索引
        m_KernelIndex = computeShader.value.FindKernel("RayTracing");

        //RT设置到核函数的RWTexture2D中
        computeShader.value.SetTexture(m_KernelIndex, "Result", m_RT);

        //todo:尝试兼容BlitTexture，目前无法叠加自定义后处理
        //将RT赋值给材质
        //片元着色器采样_MainTex，此时_MainTex是经过computeshader处理的RT
        //m_Material.SetTexture(m_MainTextureID, m_RT);
        m_Material.mainTexture = m_RT;
        
        //调度线程组，执行核函数
        int threadGrouphsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGrouphsY = Mathf.CeilToInt(Screen.height / 8.0f);
        computeShader.value.Dispatch(m_KernelIndex, threadGrouphsX, threadGrouphsY, 1);

        //进行屏幕绘制
        Blitter.BlitCameraTexture(cmd, source, destination, m_Material, 0);
        
    }

    /// <summary>
    /// //设置shader参数
    /// </summary>
    private void SetShaderParameters(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source,
        RTHandle destination)
    {
        //获取从裁剪空间转换到世界空间的矩阵
        computeShader.value.SetMatrix("_CameraToWorld", m_Camera.cameraToWorldMatrix); //获得观察空间->世界空间的矩阵
        computeShader.value.SetMatrix("_CameraInverseProjection", m_Camera.projectionMatrix.inverse); //获得裁剪空间->观察空间的矩阵
        
        //天空盒
        computeShader.value.SetTexture(m_KernelIndex, "_SkyboxTexture", SkyboxTexture.value);
        
        //抗锯齿
        computeShader.value.SetVector("_PixelOffset", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)*0.1f);
        m_AntiAliasingMaterial.SetFloat(m_Sample,m_CurrentSample);
        Blitter.BlitCameraTexture(cmd, source, destination, m_AntiAliasingMaterial, 0);
        m_CurrentSample++;
        
    }

    //释放
    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(m_RT);
        CoreUtils.Destroy(m_Material);
        CoreUtils.Destroy(m_AntiAliasingMaterial);
        computeShader.Release();
        SkyboxTexture.Release();
    }
}