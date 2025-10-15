using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Beakstorm.Rendering.RendererFeature
{
    public class BlendedSmokeRenderPassFeature : ScriptableRendererFeature
    {
        class BlendedSmokeRenderPass : ScriptableRenderPass
        {
            private const string k_PassName = "BlendedSmokePass";
            
            private Material _blitMaterial;
            private int _layerMask;
            private float _blendStrength;
            private int _blurSize;
            private float _blurSigma;

            private ShaderTagId _forwardTag = new ShaderTagId("SRPDefaultUnlit");
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

            public void Setup(Material blitMaterial, int layerMask, float blendStrength, int blurSize, float blurSigma)
            {
                _blitMaterial = blitMaterial;
                _layerMask = layerMask;
                _blendStrength = blendStrength;
                _blurSize = blurSize;
                _blurSigma = blurSigma;
            }

            private void InitRendererLists(ShaderTagId tagId, UniversalRenderingData renderingData, UniversalLightData lightData,
                ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph)
            {
                SortingCriteria sortingCriteria = passData.CameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(tagId, renderingData, passData.CameraData, lightData, sortingCriteria);

                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                filteringSettings.layerMask = _layerMask;
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;
                
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings,
                    filteringSettings);
                
                listParams.tagName = ShaderTagId.none;
                var tags = new NativeArray<ShaderTagId>(1, Allocator.Temp);
                tags[0] = ShaderTagId.none;
                
                var blocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
                RenderStateBlock stencilBlock = new RenderStateBlock(RenderStateMask.Stencil);
                stencilBlock.stencilReference = 16;
                stencilBlock.stencilState = new StencilState()
                {
                    enabled = true,
                    compareFunctionFront = CompareFunction.Always,
                    writeMask = 16,
                    readMask = 16,
                    passOperationFront = StencilOp.Replace,
                };
                blocks[0] = stencilBlock;
                listParams.stateBlocks = blocks;
                
                listParams.tagValues = tags;

                listParams.isPassTagName = false;
                
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

                //context.cmd.SetGlobalVector("_ScaledScreenParams", new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f + 1.0f / scaledCameraWidth, 1.0f + 1.0f / scaledCameraHeight));
                
                context.cmd.DrawRendererList(data.RendererListHdl);

                //context.cmd.SetGlobalVector("_ScaledScreenParams", new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f + 1.0f / scaledCameraWidth, 1.0f + 1.0f / scaledCameraHeight));
            }
            
            private class BlitPassData
            {
                public TextureHandle Source;
                public int PassIndex;
                public Material Material;
            }
            
            static void ExecuteBlitPass(BlitPassData data, RasterGraphContext context)
            {
                Blitter.BlitTexture( context.cmd, data.Source, new Vector4( 1, 1, 0, 0 ), data.Material, data.PassIndex );
            }
            
            void AddBlitPass( RenderGraph renderGraph, TextureHandle source, TextureHandle destination, TextureHandle depth, Material material, string passName, int passIndex)
            {
                using (var builder = renderGraph.AddRasterRenderPass(passName, out BlitPassData passData))
                {
                    builder.UseTexture(source);
                    passData.Source = source;
                    passData.PassIndex = passIndex;
                    passData.Material = material;
                    
                    //builder.UseTexture(edges);

                    builder.SetRenderAttachment(destination, 0);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
                    builder.SetRenderFunc<BlitPassData>((data, context) =>
                    {
                        ExecuteBlitPass(data, context);
                    });
                }
            }

            // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
            // FrameData is a context container through which URP resources can be accessed and managed.
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!_blitMaterial)
                    return;
            
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;

                
                RenderTextureDescriptor descFull = desc;
                descFull.depthStencilFormat = GraphicsFormat.None;

                
                TextureHandle destinationFullRes =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, descFull, "Destination", true);
                
                TextureHandle destinationBack =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, descFull, "DestinationBack", true);

                descFull.width /= 2;
                descFull.height /= 2;
                
                TextureHandle halfRes =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, descFull, "HalfRes 1", true);
                TextureHandle halfRes2 =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, descFull, "HalfRes 2", true);

                
                descFull.width /= 2;
                descFull.height /= 2;
                
                TextureHandle quartRes =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, descFull, "QuartRes 1", true);
                TextureHandle quartRes2 =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, descFull, "QuartRes 2", true);
                
                _blitMaterial.SetFloat("_BlendStrength", _blendStrength);
                _blitMaterial.SetFloat("_Sigma", _blurSigma);
                _blitMaterial.SetInteger("_BlurSize", _blurSize);

                RenderGraphUtils.BlitMaterialParameters blit =
                    new RenderGraphUtils.BlitMaterialParameters(destinationBack, destinationFullRes, _blitMaterial, 0);
                renderGraph.AddBlitPass(blit);
                
                // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData))
                {
                    passData.CameraData = cameraData;

                    InitRendererLists(_forwardTag, renderingData, lightData, ref passData, default, renderGraph);
                    builder.UseRendererList(passData.RendererListHdl);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    
                    // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                    //builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachment(destinationFullRes, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(resourceData.cameraDepth);
                    
                    // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        ExecutePass(data, context);
                    });

                    //resourceData.cameraColor = destination;
                }

                
                //RenderGraphUtils.BlitMaterialParameters edgeBlit = 
                //    new RenderGraphUtils.BlitMaterialParameters(destinationFullRes, , _blitMaterial, 2);
                //renderGraph.AddBlitPass(edgeBlit);
                
                renderGraph.AddBlitPass(destinationFullRes, halfRes, Vector2.one, Vector2.zero);
                renderGraph.AddBlitPass(halfRes, quartRes, Vector2.one, Vector2.zero);
                
                
                blit =
                    new RenderGraphUtils.BlitMaterialParameters(quartRes, quartRes2, _blitMaterial, 1);
                renderGraph.AddBlitPass(blit);
                
                blit =
                    new RenderGraphUtils.BlitMaterialParameters(quartRes2, quartRes, _blitMaterial, 2);
                renderGraph.AddBlitPass(blit);
                
                renderGraph.AddBlitPass(quartRes, halfRes, Vector2.one, Vector2.zero);
                
                blit =
                    new RenderGraphUtils.BlitMaterialParameters(halfRes, halfRes2, _blitMaterial, 1);
                renderGraph.AddBlitPass(blit);
                
                blit =
                    new RenderGraphUtils.BlitMaterialParameters(halfRes2, halfRes, _blitMaterial, 2);
                renderGraph.AddBlitPass(blit);

                
                renderGraph.AddBlitPass(halfRes, destinationFullRes, Vector2.one, Vector2.zero);

                blit =
                    new RenderGraphUtils.BlitMaterialParameters(destinationFullRes, destinationBack, _blitMaterial, 1);
                renderGraph.AddBlitPass(blit);
                
                blit =
                    new RenderGraphUtils.BlitMaterialParameters(destinationBack, destinationFullRes, _blitMaterial, 2);
                renderGraph.AddBlitPass(blit);

                
                blit = 
                    new RenderGraphUtils.BlitMaterialParameters(destinationFullRes, resourceData.cameraColor, _blitMaterial, 3);
               // renderGraph.AddBlitPass(blit);
                
                AddBlitPass(renderGraph, destinationFullRes, resourceData.cameraColor, resourceData.cameraDepth, _blitMaterial, "Blit Back", 3);
                
            }
        }
        
        [SerializeField] private LayerMask layerMask;

        [SerializeField] private Material blitMaterial;
        
        [SerializeField, Range(0, 1f)] 
        private float blendStrength = 1;

        [SerializeField, Range(0, 8)] 
        private int blurSize = 4;
        
        [SerializeField, Range(0, 5f)] 
        private float blurSigma = 1;
        
        
        BlendedSmokeRenderPass m_ScriptablePass;

        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new BlendedSmokeRenderPass();

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            float sigma = (blurSize * 2 + 1) / (6f * Mathf.Max(0.001f, blurSigma));
            
            m_ScriptablePass.Setup(blitMaterial, layerMask, blendStrength, blurSize, sigma);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
