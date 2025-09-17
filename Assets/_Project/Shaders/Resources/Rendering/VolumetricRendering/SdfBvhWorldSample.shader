Shader "Beakstorm/Raymarching/SDF BVH Slice"
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
        float3 worldPos : TEXCOORD2;
    };

    // Helper Functions


    // Render Functions
    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex);
        o.screenPos = ComputeScreenPos(o.vertex);
        o.worldPos = TransformObjectToWorld(v.vertex);
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

        
        // March through volume:
        float3 sdfCol = 0;
        SdfQueryInfo info = GetClosestDistance(i.worldPos, nodeOffset);

        sdfCol.r = saturate(frac(max(0, info.dist * 0.25)));
        sdfCol.g = saturate(frac(max(0, -info.dist * 0.25)));
        //sdfCol.b = info.matIndex / 4.0;
        

        float2 normalDeriveX = fwidth(info.normal.xz);
        float2 normalDeriveY = ddy(info.normal.xz);
        
        //sdfCol.rg = info.normal.xz * 0.5 + 0.5;
        //sdfCol.b = saturate(frac(max(0, info.dist)));

        float time = _Time.x;
        float2 timeVec = float2(sin(time.x), cos(time.x));
        
        //sdfCol.rg = frac(info.normal.xz);

        //sdfCol = dot(timeVec, info.normal.xz) * 0.5 + 0.5;

        float angle = atan2(timeVec.y - info.normal.z, timeVec.x - info.normal.z);
        angle = atan2(info.normal.z, info.normal.x);
        //sdfCol = frac(angle / PI * 0.5);
        sdfCol = saturate(frac(max(0, info.dist * 0.25)));
        
        sdfCol *= _Color.rgb;

        return float4(sdfCol, 1);
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry"}
        Cull Off
        ZTest LEqual
        ZWrite On 
        
        //Blend SrcAlpha OneMinusSrcAlpha

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
