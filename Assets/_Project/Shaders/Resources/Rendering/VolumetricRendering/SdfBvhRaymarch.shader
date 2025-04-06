Shader "Beakstorm/Raymarching/SDF BVH Raymarch"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(SDF Settings)]
        [Space(8)]
        _SurfaceDistance("Surface Distance", Range(-5.0, 5.0)) = 0.0
    }
    
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    
    #include "../RayUtility.hlsl"
    #include "../../Collisions/SdfCollisions.hlsl"
    
    #pragma target 3.5

    #define EPSILON 1e-4
    #define MAX_STEPS 20
    
    //Properties
    sampler2D _MainTex;
    TEXTURE2D(_CameraDepthTexture);
    SamplerState sampler_CameraDepthTexture;

    SamplerState sampler_linear_clamp;
    float3 _LightColor0;
    
    half4 _Color;

    int _NumSteps;
    float _SurfaceDistance;

    //Structs
    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float4 screenPos : TEXCOORD0;
        float3 viewNormal : TEXCOORD1;
    };

    // Helper Functions


    // Render Functions
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex);
        o.screenPos = ComputeScreenPos(o.vertex);

        // Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
        // (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
        float3 viewNormal = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
        o.viewNormal = -( GetWorldSpaceViewDir(TransformObjectToWorld(v.vertex)));
        
        return o;
    }


    float4 frag (v2f i) : SV_Target
    {
        float3 rayOrigin = _WorldSpaceCameraPos;
        float3 rayDir = normalize(i.viewNormal);

        float2 screenCoords = i.screenPos.xy / i.screenPos.w;

        float3 cameraViewNormal = mul((float3x3)unity_CameraToWorld, float3(0,0,1));

        // BIG BRAIN: dot-product of camera viewNormal with per-pixel view direction gets rid of the "fog barrier"
        // flat wall caused by the depth buffer; adds curvature to the depth 
        float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, screenCoords) * dot(rayDir, cameraViewNormal);
        float depth = LinearEyeDepth(nonLinearDepth, _ZBufferParams);;
        
        
        int nodeOffset = 0;
        float2 rayBoxInfo = GetMinimumRayDistance(rayOrigin, rayDir, 0, nodeOffset);
        
        float dstToBox = rayBoxInfo.x;
        float dstInsideBox = rayBoxInfo.y;
        float dstTravelled = EPSILON;
        float dstLimit = min(depth - dstToBox - EPSILON, dstInsideBox);

        
        // March through volume:
        float3 sdfCol = 0;

        float dist = 10000;

        int NumStepsTaken = 0;
        float alpha = 0;

        int MaximumSteps = MAX_STEPS;

        SdfQueryInfo info;
        info.dist = dist;
        info.normal = float3(0, 1, 0);
        
        while (dstTravelled < dstLimit && dist > _SurfaceDistance && NumStepsTaken < MaximumSteps)
        {
            float3 rayPos = rayOrigin + rayDir * (dstToBox + dstTravelled);
            info = GetClosestDistance(rayPos, nodeOffset);
            dist = info.dist;
            dstTravelled += dist - _SurfaceDistance;
            NumStepsTaken++;
            if(dist <= _SurfaceDistance)
            {
                sdfCol = saturate( dot(GetMainLight().direction.xyz, info.normal) ) * _Color;
                alpha = 1;
            }
        }
        if (NumStepsTaken == MaximumSteps)
        {
            sdfCol = saturate( dot(GetMainLight().direction.xyz, info.normal) ) * _Color;
            alpha = saturate(1 - dist);
        }

        clip(alpha);

        return float4(sdfCol, alpha);
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Overlay"}
        Cull Front
        ZTest Always
        ZWrite On 
        
        Blend One OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            
            ENDHLSL
        }
    }
}
