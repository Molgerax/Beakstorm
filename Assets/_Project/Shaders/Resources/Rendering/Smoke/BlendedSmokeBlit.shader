Shader "Beakstorm/BlendedSmoke/Blit"
{

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"


    SAMPLER(sampler_BlitTexture);

    
    float _BlendStrength;
    float4 _TargetSize;


    inline float4 ApplyBlendStrength(float4 col)
    {
        return col * _BlendStrength;
    }

    float GaussianBlurWeight(int x, int y, float sigma)
    {
        int sqrDist = x * x + y * y;
        float c = 2 * sigma * sigma;
        return exp(-sqrDist / c);
    }
    
    float4 GaussianBlur1D(float2 uv, float2 dir, int blurSize, float sigma)
    {
        float4 sum = 0;
        float weightSum = 0;
        float2 texelDelta = _BlitTexture_TexelSize.xy * dir;

        float4 middleSample = 0;
        
        for (int x = -blurSize; x <= blurSize; x++)
        {
            float2 uv2 = uv + texelDelta * x;
            float4 sample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv2);
            float depth = sample.r;
            float alpha = sample.a;

            
            if (x == 0)
                middleSample = sample;
            
            if (alpha > 0.5)
            {
                float weight = GaussianBlurWeight(x, 0, sigma);
                weightSum += weight;
                sum += sample * weight;
            }
        }

        if (weightSum == 0)
            return 0;

        if (middleSample.r > 10000)
            return middleSample;
        
        return sum / weightSum;
    }
    
    ENDHLSL
    
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

            TEXTURE2D(_EdgeTexture);

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                float alpha = col.a;

                return 0;
                return 1.#INF;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Gaussian Horizontal"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            float _SobelCutoff;
            float _BlurSize;
            float _Sigma;
            
            half4 Fragment(Varyings input) : SV_Target
            {
                return GaussianBlur1D(input.texcoord, float2(1, 0), _BlurSize, _Sigma);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Gaussian Vertical"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            float _SobelCutoff;
            float _BlurSize;
            float _Sigma;
            
            half4 Fragment(Varyings input) : SV_Target
            {
                //return SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.texcoord);
                return GaussianBlur1D(input.texcoord, float2(0, 1), _BlurSize, _Sigma);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Blit Back"
            ZTest Always
            ZWrite Off
            Cull Off
            
            Stencil 
            {
                Ref 16
                ReadMask 16
                Comp Equal
            }  
            
            //Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend SrcAlpha OneMinusSrcAlpha

            
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

            
	        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	        TEXTURE2D(_CameraDepthAttachment);
            
            float4 ViewPos(float2 uv)
            {
                float depth = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv).r;
                float3 viewVector = mul(unity_CameraInvProjection, float4(uv.xy * 2 - 1, 0, 1));
                return float4(normalize(viewVector) * depth, depth);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                float alpha = col.a;

                float3 viewVector = mul((float3x3)unity_CameraToWorld, float3(0,0,1));;
                
                float4 posCenter = ViewPos(uv);
                if (posCenter.a > 10000)
                {
                    return 0;
                }



                float2 stepSize = _BlitTexture_TexelSize.xy;
                float3 ddx = ViewPos(uv + float2(stepSize.x, 0)) - posCenter;
                float3 ddx2 = posCenter - ViewPos(uv + float2(-stepSize.x, 0));
                if (abs(ddx2.z) < abs(ddx.z))
                {
                    ddx = ddx2;
                }
                
                float3 ddy = ViewPos(uv + float2(0, stepSize.y)) - posCenter;
                float3 ddy2 = posCenter - ViewPos(uv + float2(0,-stepSize.y));
                if (abs(ddy2.z) < abs(ddy.z)) {
                    ddy = ddy2;
                }
                
                float3 viewNormal = normalize(cross(ddy, ddx));
                float3 worldNormal = TransformViewToWorldNormal(viewNormal);

                //worldNormal = viewNormal;
                //worldNormal = normalize(col.rgb);
                //viewNormal = TransformWorldToViewNormal(worldNormal, true);

                
                Light light = GetMainLight();
                col.rgb = worldNormal * 0.5 + 0.5;

                //return col;
                
                float3 rimLightDir = normalize(cross(float3(0,1,0), light.direction));
                
                float rimLight = dot(-viewVector, worldNormal);
                
                float l = dot(light.direction, worldNormal);

                float steppedLight = floor(l * 3) / 3;

                
                col.rgb = lerp(0.3, 0.8, steppedLight);
                col.rgb = lerp(col.rgb, 0, step(rimLight, 0.2));


                col.rgb = saturate(-l);
                
                //#ifdef _LINEAR_TO_SRGB_CONVERSION
                //col = LinearToSRGB(col);
                //#endif


                col = ApplyBlendStrength(col);
                
                return col;
            }
            ENDHLSL
        }
    }
}
