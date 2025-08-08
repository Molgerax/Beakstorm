using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Beakstorm.Rendering.RendererFeature
{
    public class PheromoneRenderPassFeature : ScriptableRendererFeature
    {
        class PheromoneRenderPass : ScriptableRenderPass
        {
            private const string k_PassName = "PheromoneParticlePass";
            private Material _material;
            
            private int _downSample;
            private int _layerMask;
            private float _blendStrength;
            private float _sobelCutoff;
            private bool _renderSecond;
            private Material _blitMaterial;

            private ShaderTagId _forwardTag = new ShaderTagId("UniversalForward");
            private ShaderTagId _stencilTag = new ShaderTagId("UniversalForwardStencil");
            

            // This class stores the data needed by the RenderGraph pass.
            // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
            private class PassData
            {
                public UniversalCameraData CameraData;
                public RendererListHandle RendererListHdl;
                public TextureHandle LowResDepth;
                public TextureHandle Edges;
                public int DownSample;
            }

            public void Setup(Material blitMaterial, int layerMask, int downSample, float blendStrength, float sobelCutoff, bool renderSecond)
            {
                _blitMaterial = blitMaterial;
                _downSample = 1 << (downSample);
                _layerMask = layerMask;
                _blendStrength = blendStrength;
                _sobelCutoff = sobelCutoff;
                _renderSecond = renderSecond;
            }

            private void InitRendererLists(ShaderTagId tagId, UniversalRenderingData renderingData, UniversalLightData lightData,
                ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph)
            {
                SortingCriteria sortingCriteria = passData.CameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(tagId, renderingData, passData.CameraData, lightData, sortingCriteria);

                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                filteringSettings.layerMask = _layerMask;
                filteringSettings.renderQueueRange = RenderQueueRange.transparent;
                
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings,
                    filteringSettings);
                
                passData.RendererListHdl = renderGraph.CreateRendererList(listParams);
            }

            // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
            // It is used to execute draw commands.
            static void ExecutePass(PassData data, RasterGraphContext context)
            {
                Rect pixelRect = data.CameraData.camera.pixelRect;
                float renderScale = data.CameraData.isSceneViewCamera ? 1f : data.CameraData.renderScale;
                float scaledCameraWidth = (float)pixelRect.width * renderScale / data.DownSample;
                float scaledCameraHeight = (float)pixelRect.height * renderScale / data.DownSample;

                context.cmd.SetGlobalVector("_ScaledScreenParams", new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f + 1.0f / scaledCameraWidth, 1.0f + 1.0f / scaledCameraHeight));

                
                context.cmd.SetGlobalTexture("_CameraDepthAttachment", data.LowResDepth);
                context.cmd.DrawRendererList(data.RendererListHdl);

                context.cmd.SetGlobalVector("_ScaledScreenParams", new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f + 1.0f / scaledCameraWidth, 1.0f + 1.0f / scaledCameraHeight));
            }
            
            private class BlitPassData
            {
                public TextureHandle Source;
                public TextureHandle Edges;
                public int PassIndex;
                public Material Material;
            }
            
            static void ExecuteBlitPass(BlitPassData data, RasterGraphContext context)
            {
                data.Material.SetTexture("_EdgeTexture", data.Edges);
                Blitter.BlitTexture( context.cmd, data.Source, new Vector4( 1, 1, 0, 0 ), data.Material, data.PassIndex );
            }
            
            void AddBlitPass( RenderGraph renderGraph, TextureHandle source, TextureHandle destination, TextureHandle depth, Material material, string passName, int passIndex, TextureHandle edges )
            {
                using (var builder = renderGraph.AddRasterRenderPass(passName, out BlitPassData passData))
                {
                    builder.UseTexture(source);
                    passData.Source = source;
                    passData.PassIndex = passIndex;
                    passData.Material = material;
                    
                    
                    passData.Edges = edges;
                    builder.UseTexture(edges);

                    builder.SetRenderAttachment(destination, 0);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
                    builder.SetRenderFunc<BlitPassData>((data, context) =>
                    {
                        ExecuteBlitPass(data, context);
                    });
                }
            }
            
            public class TextureBindInfo
            {
                public int slot;
                public TextureHandle texture;
            }


            // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
            // FrameData is a context container through which URP resources can be accessed and managed.
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                RenderTextureDescriptor descFull = desc;
                descFull.depthStencilFormat = GraphicsFormat.None;
                
                TextureHandle destinationFullRes =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, descFull, "Destination FullRes Texture", true);

                desc.width /= _downSample;
                desc.height /= _downSample;
                RenderTextureDescriptor depthDesc = desc;    
                desc.depthStencilFormat = GraphicsFormat.None;
                
                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Destination Texture", true);
                //desc.colorFormat = RenderTextureFormat.;
                TextureHandle edges = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Edge Detection Texture", true);
                
                
                depthDesc.colorFormat = RenderTextureFormat.Depth;
                TextureHandle lowDepth = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                    depthDesc, "Low Res Depth", true);

                depthDesc.depthStencilFormat = GraphicsFormat.None;
                depthDesc.colorFormat = RenderTextureFormat.RFloat;
                TextureHandle lowDepth2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                    depthDesc, "Low Res Depth2", true);
                
                _blitMaterial.SetVector("_TargetSize", new Vector4(1f / desc.width, 1f / desc.height, desc.width, desc.height));
                
                RenderGraphUtils.BlitMaterialParameters blitDepth =
                    new RenderGraphUtils.BlitMaterialParameters(resourceData.cameraDepth, lowDepth, _blitMaterial, 1);
                renderGraph.AddBlitPass(blitDepth);
                
                blitDepth = 
                    new RenderGraphUtils.BlitMaterialParameters(resourceData.cameraDepthTexture, lowDepth2, _blitMaterial, 1);
                renderGraph.AddBlitPass(blitDepth);
                
                _blitMaterial.SetFloat("_SobelCutoff", _sobelCutoff);
                _blitMaterial.SetFloat("_BlendStrength", _blendStrength);
                
                // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData))
                {
                    passData.CameraData = cameraData;
                    passData.DownSample = _downSample;
                    passData.Edges = edges;
                    
                    InitRendererLists(_forwardTag, renderingData, lightData, ref passData, default, renderGraph);
                    builder.UseRendererList(passData.RendererListHdl);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.UseTexture(lowDepth2);
                    
                    // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                    //builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(lowDepth, AccessFlags.Read);

                    passData.LowResDepth = lowDepth2;
                    
                    // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(data, context);
                    });

                    //resourceData.cameraColor = destination;
                }

                
                RenderGraphUtils.BlitMaterialParameters edgeBlit = 
                    new RenderGraphUtils.BlitMaterialParameters(destination, edges, _blitMaterial, 2);
                renderGraph.AddBlitPass(edgeBlit);
                
                AddBlitPass(renderGraph, destination, resourceData.cameraColor, resourceData.cameraDepth, _blitMaterial, "Blit With Edge", 0, edges);
                
                if (!_renderSecond)
                    return;
                
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData))
                {
                    passData.CameraData = cameraData;
                    passData.DownSample = 1;
                    passData.Edges = edges;
                    
                    InitRendererLists(_stencilTag, renderingData, lightData, ref passData, default, renderGraph);
                    builder.UseRendererList(passData.RendererListHdl);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.UseTexture(resourceData.cameraDepthTexture);
                    
                    // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                    //builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachment(destinationFullRes, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(resourceData.cameraDepth, AccessFlags.Read);

                    passData.LowResDepth = resourceData.cameraDepthTexture;
                    
                    // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(data, context);
                    });

                    //resourceData.cameraColor = destination;
                }
                
                RenderGraphUtils.BlitMaterialParameters blit =
                    new RenderGraphUtils.BlitMaterialParameters(destinationFullRes, resourceData.cameraColor, _blitMaterial, 3);
                
                renderGraph.AddBlitPass(blit);
            }
        }
        
        [SerializeField] private LayerMask layerMask;
        [SerializeField]
        [Range(0, 3)] private int downSampleFactor = 1;

        [SerializeField] private Material blitMaterial;
        
        [SerializeField, Range(0, 1f)] 
        private float blendStrength = 1;
        
        [SerializeField, Range(0, 1f)] 
        private float sobelCutoff;

        [SerializeField] 
        private bool renderSecond;
        
        
        PheromoneRenderPass m_ScriptablePass;

        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new PheromoneRenderPass();

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(blitMaterial, layerMask, downSampleFactor, blendStrength, sobelCutoff, renderSecond);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
