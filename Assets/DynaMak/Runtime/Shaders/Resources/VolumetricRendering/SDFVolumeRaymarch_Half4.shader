Shader "DynaMak/Raymarching/SDFVolumeRaymarch"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(SDF Settings)]
        [Space(8)]
        _SurfaceDistance("Surface Distance", Range(-5.0, 5.0)) = 0.0
    }
    
    CGINCLUDE

    #include "UnityCG.cginc"
    #include "../VolumeUtility.hlsl"
    #include "../SDFUtility.hlsl"
    #include "../RayUtility.hlsl"

    #define EPSILON 1e-4
    #define MAX_STEPS 200
    
    //Properties
    sampler2D _MainTex;
    sampler2D _CameraDepthTexture;
    float3 _LightColor0;
    
    half4 _Color;

    Texture3D<half4> _Volume;
    float3 _VolumeCenter;
    float3 _VolumeBounds;

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
    half4 sampleSDF(float3 position)
    {
        return  Sample_Volume(_Volume, _VolumeCenter, _VolumeBounds, position);
    }


    // Render Functions
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.screenPos = ComputeScreenPos(o.vertex);

        // Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
        // (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
        float3 viewNormal = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
        o.viewNormal = -( WorldSpaceViewDir(v.vertex));
        
        return o;
    }


    fixed4 frag (v2f i) : SV_Target
    {
        float3 rayOrigin = _WorldSpaceCameraPos;
        float3 rayDir = normalize(i.viewNormal);

        float2 screenCoords = i.screenPos.xy / i.screenPos.w;

        float3 cameraViewNormal = mul((float3x3)unity_CameraToWorld, float3(0,0,1));

        // BIG BRAIN: dot-product of camera viewNormal with per-pixel view direction gets rid of the "fog barrier"
        // flat wall caused by the depth buffer; adds curvature to the depth 
        float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenCoords) * dot(rayDir, cameraViewNormal);
        float depth = LinearEyeDepth(nonLinearDepth);
        
        
        float2 rayBoxInfo = rayBoxDist(_VolumeCenter - _VolumeBounds, _VolumeCenter + _VolumeBounds, rayOrigin, rayDir);

        float dstToBox = rayBoxInfo.x;
        float dstInsideBox = rayBoxInfo.y;
        float dstTravelled = EPSILON;
        float dstLimit = min(depth - dstToBox - EPSILON, dstInsideBox);
        // March through volume:

        float3 sdfCol = 0;

        half4 dist = half4(0,0,0, 10000);

        int NumStepsTaken = 0;
        float alpha = 0;

        int MaximumSteps = _NumSteps * 5;
        
        while (dstTravelled < dstLimit && dist.w > _SurfaceDistance && NumStepsTaken < MaximumSteps)
        {
            float3 rayPos = rayOrigin + rayDir * (dstToBox + dstTravelled);
            dist = sampleSDF(rayPos);
            dstTravelled += dist.w - _SurfaceDistance;
            NumStepsTaken++;
            if(dist.w <= _SurfaceDistance)
            {
                sdfCol = saturate( dot(_WorldSpaceLightPos0.xyz, normalize(dist.xyz)) ) * _Color;
                alpha = 1;
            }
        }
        if (NumStepsTaken == MaximumSteps)
        {
            sdfCol = saturate( dot(_WorldSpaceLightPos0.xyz, normalize(dist.xyz)) ) * _Color;
            alpha = 1;
        }

        clip(alpha);

        return float4(sdfCol, alpha);
    }
    
    ENDCG
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay"}
        Cull Front
        ZTest Always
        ZWrite On 
        
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            
            ENDCG
        }
    }
}
