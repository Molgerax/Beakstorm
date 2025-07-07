Shader "Beakstorm/Pheromone/Blit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "Blit"
            ZTest Always
            ZWrite Off
            Cull Off
            
            //Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend One OneMinusSrcAlpha

            Stencil 
            {
                Ref 1
                Comp Always
                Pass Replace
            }    
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            // Core.hlsl for XR dependencies
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_EdgeTexture);

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float4 edge = SAMPLE_TEXTURE2D(_EdgeTexture, sampler_BlitTexture, uv);

                if (edge.r > 0.0)
                    discard;
                
                
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                float alpha = col.a;

                #ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
                #endif
                
                return col;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthBlit"
            ZTest Always
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            // Core.hlsl for XR dependencies
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            SAMPLER(sampler_BlitTexture);
            
            half4 Fragment(Varyings input, out float outDepth : SV_Depth) : SV_Target
            {
                float2 uv = input.texcoord;

                //float depth = SAMPLE_DEPTH_TEXTURE(_BlitTexture, sampler_BlitTexture, uv);

                int sampleCount = 2;

                float2 factor = _BlitTexture_TexelSize.xy; 
                
                float depth = 0;
                //depth = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_PointClamp, uv, 0).x;

                for (int x = 0; x < sampleCount; x++)
                {
                    for (int y = 0; y < sampleCount; y++)
                    {
                        float2 uvTest = (float2(x,y) / (sampleCount - 1) - 0.5) * factor;

                        if (sampleCount <= 1)
                            uvTest = 0;

                        uvTest += uv;
                        
                        float d = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_PointClamp, uvTest, 0).x;
                        depth = max(d, depth);
                        //depth += d * (1.0 / sampleCount / sampleCount);
                    }
                }
                

                outDepth = depth;
                return depth;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Sobel"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            // Core.hlsl for XR dependencies
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            SAMPLER(sampler_BlitTexture);

            float _SobelCutoff;
            
            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float3 offset = 0;
                offset.x = _BlitTexture_TexelSize.x;
                offset.y = _BlitTexture_TexelSize.y;
                

                
                float4 pixel11 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointRepeat, uv + offset.zz);
                float4 pixel01 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointRepeat, uv - offset.xz);
                float4 pixel21 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointRepeat, uv + offset.xz);
                float4 pixel10 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointRepeat, uv - offset.zy);
                float4 pixel12 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointRepeat, uv + offset.zy);

                float4 sobel =
                    abs(pixel01 - pixel11) + 
                    abs(pixel21 - pixel11) + 
                    abs(pixel10 - pixel11) + 
                    abs(pixel12 - pixel11);

                sobel = pixel01 + pixel10 - pixel21 - pixel12;
                sobel /= 4;
                
                float output = max(max(sobel.x, sobel.y), max(sobel.z, sobel.w));
                output = sobel.a;
                output = max( abs(pixel01 - pixel21) / 2, abs(pixel10 - pixel12) / 2);

                output = 1 - step(output, _SobelCutoff);

                return output;

                output = step(sobel.a, 0);
                output = pixel11.a;


                
                return output;
            }
            ENDHLSL
        }
    }
}
