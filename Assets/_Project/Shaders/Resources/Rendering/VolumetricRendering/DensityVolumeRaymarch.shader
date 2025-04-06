Shader "DynaMak/Raymarching/DensityVolumeRaymarch"
{
    Properties
    {
        _Color ("Fog Color", Color) = (1,1,1,1)
        
        [Header(Density Settings)]
        [Space(8)]
        _DensityThreshold("Density Threshold", Range(0.0, 5.0)) = 0.0
        _DensityMultiplier("Density Mutliplier", Range(0.0, 10.0)) = 1.0
        
        [Header(Light Absorption Settings)]
        [Space(8)]
        _LightAbsorption("Light Absorption", Float) = 2.0
        _LightSourceAbsorption("Light Source Absorption", Float) = 2.0
        _DarkThreshold("Dark Threshold", Range(0.0, 1.0)) = 0.1
        
        [Toggle(ATTENUATION)] _UseAttenuation("Attenuation & Shadow Sampling", Int) = 0
        [Toggle(AMBIENT_LIGHT)] _AmbientLightSH("Ambient Light from Skybox", Int) = 0
    }
    
    CGINCLUDE

    #include "UnityCG.cginc"
    #include "AutoLight.cginc"
    #include "Lighting.cginc"
    
    #include "../VolumeUtility.hlsl"
    #include "../RayUtility.hlsl"
    #include "../Commons.cginc"

    #pragma target 3.5
	#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
    #pragma multi_compile_fwdadd_fullshadows
    #pragma shader_feature _ ATTENUATION
    #pragma shader_feature _ AMBIENT_LIGHT
    #pragma skip_variants POINT_COOKIE DIRECTIONAL_COOKIE SPOT SPOT_COOKIE SHADOWS_SCREEN LIGHTMAP_SHADOW_MIXING

    #define EPSILON 1e-4


    #pragma multi_compile _ HALF4
    
    //Properties
    sampler2D _CameraDepthTexture;
    
    half4 _Color;

    
    #if HALF4
    Texture3D<half4> _Volume;
    #else
    Texture3D<half> _Volume;
    #endif
    
    float3 _VolumeCenter;
    float3 _VolumeBounds;


    float _DensityThreshold;
    float _DensityMultiplier;
    int _NumSteps;
    int _NumStepsLight;
    float _RandomSampleOffset;

    float _LightAbsorption;
    float _LightSourceAbsorption;
    float _DarkThreshold;

    //Structs
    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float4 screenPos : TEXCOORD0;
        float3 viewNormal : TEXCOORD1;
        float3 cameraViewNormal : TEXCOORD2;
        float3 worldPos : TEXCOORD3;

        LIGHTING_COORDS(4,5)
    };




    // Helper Functions
    float sampleDensity(float3 position)
    {
        #if HALF4
        half fogSample = Sample_Volume(_Volume, _VolumeCenter, _VolumeBounds, position).w;
        #else
        half fogSample = Sample_Volume(_Volume, _VolumeCenter, _VolumeBounds, position);
        #endif
        
        float density = max(0, fogSample - _DensityThreshold) * _DensityMultiplier;
        return density;
    }

    float sampleAlongRay(float3 rayOrigin, float3 rayDir, int steps)
    {
        float dstInsideBox = rayBoxDist(_VolumeCenter - _VolumeBounds * 0.5, _VolumeCenter + _VolumeBounds * 0.5, rayOrigin, rayDir).y;
        float dist = dstInsideBox;

        float stepSize = dist / max(1,steps);
        float totalDensity = 0;
        float transmittance = 1;

        for (int step = 0; step < steps; step ++) {
            float density = max(0, sampleDensity(rayOrigin) * stepSize);
            totalDensity += density;
            transmittance *= exp(-density * stepSize * _LightSourceAbsorption);
            
            rayOrigin += rayDir * stepSize;
        }
        transmittance = exp(-totalDensity * _LightSourceAbsorption);

        return _DarkThreshold + transmittance * (1 - _DarkThreshold);
    }

    float attenuation(float dist)
    {
        return saturate( 1.0 / (1.0 + dist * dist));
    }
    

    float lightmarch(float3 position)
    {
        float3 vertexToLight = _WorldSpaceLightPos0.xyz - position;
        float3 dirToLight = lerp(_WorldSpaceLightPos0.xyz, vertexToLight, _WorldSpaceLightPos0.w);
        if(dot(dirToLight, dirToLight) == 0) return 0;
        dirToLight = normalize(dirToLight);
                
        float dstInsideBox = rayBoxDist(_VolumeCenter - _VolumeBounds * 0.5, _VolumeCenter + _VolumeBounds * 0.5, position, dirToLight).y;

        float dist = lerp(dstInsideBox, min(dstInsideBox,length(vertexToLight)), _WorldSpaceLightPos0.w);

        //dist = dstInsideBox;
        
        float stepSize = dist / _NumStepsLight;
        float totalDensity = 0;
        float transmittance = 1;

        float randomOffset = Random( float(vertexToLight.x * 723.1 + dot(position.zyxz, _Time.xywz))) * stepSize;
        //position += _RandomSampleOffset * dirToLight * randomOffset;
        //stepSize = (dist - _RandomSampleOffset * randomOffset) / _NumStepsLight;
        
        for (int step = 0; step < _NumStepsLight; step ++) {
            float density = max(0, sampleDensity(position) * stepSize);
            totalDensity += density;
            transmittance *= exp(-density * stepSize * _LightSourceAbsorption);
            
            position += dirToLight * stepSize;
        }
        transmittance = exp(-totalDensity * _LightSourceAbsorption);

        return _DarkThreshold + transmittance * (1 - _DarkThreshold);
    }

    // Render Functions
    v2f vert (appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.screenPos = ComputeScreenPos(o.pos);
        o.worldPos = _VolumeCenter;
        //o.worldPos = mul(unity_ObjectToWorld, v.vertex);

        // Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
        // (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
        float3 viewNormal = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
        o.viewNormal = -( WorldSpaceViewDir(v.vertex));
        o.cameraViewNormal = mul((float3x3)unity_CameraToWorld, float3(0,0,1));

        TRANSFER_VERTEX_TO_FRAGMENT(o)
        return o;
    }


    fixed4 fragBase (v2f i) : SV_Target
    {
        float3 rayOrigin = _WorldSpaceCameraPos;
        float3 rayDir = normalize(i.viewNormal);

        float2 screenCoords = i.screenPos.xy / i.screenPos.w;

        // BIG BRAIN: dot-product of camera viewNormal with per-pixel view direction gets rid of the "fog barrier"
        // flat wall caused by the depth buffer; adds curvature to the depth 
        float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenCoords) * dot(rayDir, i.cameraViewNormal);
        float depth = LinearEyeDepth(nonLinearDepth);

        //return float4(depth.x * 0.1, 0, 0, 0);

        
        float2 rayBoxInfo = rayBoxDist(_VolumeCenter - _VolumeBounds * 0.5, _VolumeCenter + _VolumeBounds * 0.5, rayOrigin, rayDir);

        float dstToBox = rayBoxInfo.x;
        float dstInsideBox = rayBoxInfo.y;
        float dstTravelled = EPSILON;

        float dstToTravel = min(depth - max(0,dstToBox) - EPSILON, dstInsideBox);
        
        float stepSize = dstToTravel / _NumSteps;
        float randomOffset = Random( float(screenCoords.x * 723.1 + dot(screenCoords.xyx, _Time.xyw))) * stepSize;
        dstTravelled += _RandomSampleOffset * randomOffset;
        
        // March through volume:
        float totalLight = 0;
        float transmittance = 1;

        half3 ambientLightTotal = 0;

#if ATTENUATION
        [unroll(30)]
#endif
        for (int step = 0; step < _NumSteps; step++) {
            float3 rayPos = rayOrigin + rayDir * (dstToBox + dstTravelled);
            float lightTransmittance = lightmarch(rayPos);
            float density = sampleDensity(rayPos);

#if ATTENUATION
            UNITY_LIGHT_ATTENUATION(attenuation, i, rayPos)
#else
            fixed attenuation = 1;
#endif


#if AMBIENT_LIGHT
            half4 n = 1;
            int maxAmbientSamples = 1;
            for (int sampleCounter = 0; sampleCounter < maxAmbientSamples; sampleCounter++)
            {
                n.xyz = RandomUnitVector(float(randomOffset * (4 + step) + sampleCounter + _Time.y));
                float ambientTransmittance = sampleAlongRay(rayPos, n.xyz, _NumStepsLight);
                ambientLightTotal += density * ambientTransmittance * stepSize * transmittance * ShadeSH9(n) * _Color / maxAmbientSamples;
            }
#endif
            totalLight += density * lightTransmittance * stepSize * transmittance * attenuation;
            transmittance *= exp(-density * stepSize * _LightAbsorption);
            dstTravelled += stepSize;
        }

        float alpha = 1 - transmittance;

        
        float3 diffLight = totalLight * _LightColor0 * _Color;
        float3 ambientLight = half3(unity_SHAr.w,unity_SHAg.w,unity_SHAb.w) * _Color * alpha; 
        
#if AMBIENT_LIGHT
        ambientLight = ambientLightTotal;
#        endif

        float3 cloudCol = diffLight + ambientLight;
        
        return float4(cloudCol, transmittance);
    }

    fixed4 fragAdd (v2f i) : SV_Target
    {
        float3 rayOrigin = _WorldSpaceCameraPos;
        float3 rayDir = normalize(i.viewNormal);

        float2 screenCoords = i.screenPos.xy / i.screenPos.w;

        // BIG BRAIN: dot-product of camera viewNormal with per-pixel view direction gets rid of the "fog barrier"
        // flat wall caused by the depth buffer; adds curvature to the depth 
        float nonLinearDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenCoords) * dot(rayDir, i.cameraViewNormal);
        float depth = LinearEyeDepth(nonLinearDepth);

        //return float4(depth.x * 0.1, 0, 0, 0);
        
        float2 rayBoxInfo = rayBoxDist(_VolumeCenter - _VolumeBounds * 0.5, _VolumeCenter + _VolumeBounds * 0.5, rayOrigin, rayDir);

        float dstToBox = rayBoxInfo.x;
        float dstInsideBox = rayBoxInfo.y;
        float dstTravelled = EPSILON;
        
        float dstToTravel = min(depth - max(0,dstToBox) - EPSILON, dstInsideBox);
        
        float stepSize = dstToTravel / _NumSteps;
        dstTravelled += _RandomSampleOffset * (Random( float(screenCoords.x * 723.1 + screenCoords.y))) * (stepSize);
        

        // March through volume:
        float totalLight = 0;
        float transmittance = 1;

        
#if ATTENUATION
        [unroll(30)]
#endif        
        for (int step = 0; step < _NumSteps; step++) {
            float3 rayPos = rayOrigin + rayDir * (dstToBox + dstTravelled);
            float lightTransmittance = lightmarch(rayPos);
            float density = sampleDensity(rayPos);

            #if ATTENUATION
            UNITY_LIGHT_ATTENUATION(attenuation, i, rayPos)
            #else
            fixed attenuation = 1;
            #endif
            
            totalLight += density * lightTransmittance * stepSize * transmittance * attenuation;
            transmittance *= exp(-density * stepSize * _LightAbsorption);
            dstTravelled += stepSize;
        }

        float alpha = 1 - transmittance;
        
        
        float3 diffLight = totalLight * _LightColor0 * _Color;
        
        float3 cloudCol = diffLight * alpha;
        
        return float4(cloudCol, 1);
    }
    
    ENDCG
    
    SubShader
    {
        Tags { "Queue" = "AlphaTest"}
        
        Pass // Base with Ambient Light
        {
            Tags { "LightMode" = "ForwardBase" "RenderType" = "Transparent"}
            
            Cull Front
            ZTest Always
            ZWrite Off 
        
            Blend One SrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBase

            
            ENDCG
        }
        
        Pass //Additional Lights
    	{
	        Tags { "LightMode" = "ForwardAdd" "RenderType"="Transparent"}
        	
    	    Cull Front
    	    ZTest Always
    	    ZWrite Off
    	    
    	    Blend One SrcAlpha
	
        	
        	CGPROGRAM
	
        	#pragma vertex vert
			#pragma fragment fragAdd
        	
        	ENDCG
		}
    }
}
