Shader "BeakStorm/Pheromones/Raymarch"
{
    Properties
    {
        _MainColor("Color", Color) = (1, 1, 1, 1)
        _OffColor("Color", Color) = (1, 1, 1, 1)
    	_MainTex("Texture", 2D) = "white" {}
    	_VertexColorToBase("Vertex Color Mask for Base", Range(0.0,1.0)) = 1
    	
        [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
    	
    	[HDR] _EmissiveColor("Emission Color", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _EmissiveMap("Emission Map", 2D) = "white" {}
    	_VertexColorToEmissive("Vertex Color Mask for Emission", Range(0.0,1.0)) = 1
    	_Size("Size", Float) = 1
    	_ZFade("ZFade", Range(0.001,1)) = 0.1
    	
        [Toggle] _SORT("Use Sorting Buffer", Integer) = 1
    	
    	_LightAbsorption("Light Absorption", Float) = 1
    }
    
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    
    #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
    #include "UnityIndirect.cginc"

    #include "../../Pheromones/PheromoneMath.hlsl"
    #include "../../SpatialGrid/SpatialGridSampling.hlsl"
    
    #pragma target 3.5
    #pragma shader_feature _ _SHADOWMODE_ON
	#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
    #pragma multi_compile_fwdadd_fullshadows
    #pragma shader_feature _ _SORT_ON
    
    struct v2f
    {
        float4 pos				: SV_POSITION;
		half4 color			: COLOR;
    	float2 uv				: TEXCOORD0;
		float3 normal			: TEXCOORD1;
    	float4 tangent			: TEXCOORD2;
    	float3 worldPos			: TEXCOORD3;
    	//LIGHTING_COORDS(4,5)
    	
    	
        #if _SHADOWMODE_ON
			float4 shadowCoord	: COLOR1;
		#endif
    };
    
    //Properties
    int _SubMeshId;

    CBUFFER_START(UnityPerMaterial)
    
    TEXTURE2D(_MainTex);
    float4 _MainTex_ST;
    sampler2D _NormalMap;
	sampler2D _EmissiveMap;

    
    half4 _MainColor;
    half4 _OffColor;
    float _VertexColorToBase;
    float3 _EmissiveColor;
	float _VertexColorToEmissive;
	float _Size;
	float _ZFade; 

	float _LightAbsorption;
    
    struct Pheromone
	{
	    float3 pos;
	    float life;
	    float3 oldPos;
	    float maxLife;
	    float4 data;
	};

    struct SortEntry
    {
	    uint index;
    	float dist;
    };
    
    StructuredBuffer<Pheromone> _PheromoneBuffer;
    StructuredBuffer<SortEntry> _PheromoneSortingBuffer;

    StructuredBuffer<uint> _PheromoneSpatialOffsets;
 
    CBUFFER_END

    
	
	    // Pull in URP library functions and our own common functions
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

	TEXTURE2D(_CameraDepthAttachment);
    
	// Textures
	SAMPLER(sampler_MainTex);
	
	float4 _ColorMap_ST; // This is automatically set by Unity. Used in TRANSFORM_TEX to apply UV tiling
	float4 _ColorTint;
	float _Smoothness;
	
	// This attributes struct receives data about the mesh we're currently rendering
	// Data is automatically placed in fields according to their semantic
	struct Attributes {
		float3 positionOS : POSITION; // Position in object space
		float3 normalOS : NORMAL; // Normal in object space
		float2 uv : TEXCOORD0; // Material texture UVs
	};
	
	// This struct is output by the vertex function and input to the fragment function.
	// Note that fields will be transformed by the intermediary rasterization stage
	struct Interpolators {
		// This value should contain the position in clip space (which is similar to a position on screen)
		// when output from the vertex function. It will be transformed into pixel position of the current
		// fragment on the screen when read from the fragment function
		float4 positionCS : SV_POSITION;
		half4 color			: COLOR;
	
		// The following variables will retain their values from the vertex stage, except the
		// rasterizer will interpolate them between vertices
		float2 uv			: TEXCOORD0;
		float2 screenUV		: TEXCOORD1;
		float3 positionWS	: TEXCOORD2;
		uint instanceID		: SV_InstanceID;
		float depth : TEXCOORD3;
	};
	
	float3 hsv2rgb(float3 hsv)
	{
		float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
		float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
	
		return hsv.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);
	}

    float LinearDepthToNonLinear(float linear01Depth, float4 zBufferParam){
		// Inverse of Linear01Depth
		return (1.0 - (linear01Depth * zBufferParam.y)) / (linear01Depth * zBufferParam.x);
	}
	
	float EyeDepthToNonLinear(float eyeDepth, float4 zBufferParam){
		// Inverse of LinearEyeDepth
		return (1.0 - (eyeDepth * zBufferParam.w)) / (eyeDepth * zBufferParam.z);
	}

    
	float NumberToDots(float number, float2 uv)
	{
		int2 id = floor(uv * 16);
	
	
		if (id.x % 2 == 0 || id.y % 2 == 0)
			return 0;
		
		int count = (id.x / 2) + id.y / 2 * 16;
	
		if (count <= number * 16)
			return 1;
		return 0;
	}

    float GetSize(Pheromone p)
	{
		return (0.25 + p.maxLife - p.life) * _Size;
	}

    float SampleDensityAtPoint(float3 pos, float smoothingRadius)
	{
    	int numNeighbors = 0;
    	float3 densityGradient = 0;
    	float density = 0;
    	
    	int3 originCell = GetGridCellId(pos, _HashCellSize, _SimulationCenter, _SimulationSize);
    	
    	int sideLength = GetCellCoverageSideLength(smoothingRadius, _HashCellSize);
    	int3 cellOffset = GetCellOffset(pos, sideLength, _HashCellSize);
    	
    	for(int iterator = 0; iterator < sideLength * sideLength * sideLength; iterator++)
    	{
    	    int3 offset3D = GetIntegerOffsets3D(sideLength, iterator) + cellOffset;
    	    
    	    uint key = KeyFromCellId(originCell + offset3D, _CellDimensions);
    	    uint currIndex = _PheromoneSpatialOffsets[key-1];
    	    uint nextIndex = _PheromoneSpatialOffsets[key+0];
    	    
    	    while (currIndex < nextIndex)
    	    {
    	        Pheromone p2 = _PheromoneBuffer[currIndex];
    	        currIndex++;
    	        
    	        if (p2.life <= 0)
    	            continue;
    	        
    	        float3 positionB = p2.pos;
    	        float3 offset = positionB - pos;
    	        float distSquared = dot(offset, offset);
    	        
    	        if (distSquared > smoothingRadius * smoothingRadius)
    	            continue;
    	        numNeighbors++;
	
    	        float d = GetDensityFromParticle(pos, positionB, smoothingRadius);
    	        //float3 g = GetDensityDerivativeFromParticle(pos, positionB, smoothingRadius);
    	        
    	        density += d;
    	        //densityGradient += g;
    	    }
    	}
		return density;
	}
    
	Interpolators Vertex(Attributes input, uint instance_id: SV_InstanceID)
	{
		Interpolators output = (Interpolators)0;

		InitIndirectDrawArgs(0);
        uint instanceID = GetIndirectInstanceID(instance_id);
		uint index = instanceID;
		
		#if _SORT_ON
		SortEntry entry = _PheromoneSortingBuffer[instanceID];
		index = entry.index;
		#endif
		
		Pheromone pheromone = _PheromoneBuffer[index];
		if (pheromone.life <= 0)
			return (Interpolators)(1.0 / 0.0);
		
		float3 meshPositionWS = pheromone.pos;
		float4 data = pheromone.data;
		

		float3 vpos = input.positionOS.xyz * GetSize(pheromone);
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;

		float random = GenerateHashedRandomFloat(pheromone.data.y);
		
		float angle = (random * 2 - 1) * pheromone.data.w * 6.28;
		float sine, cosine;
		sincos(angle, sine, cosine);
		
		float3x3 tilt = float3x3(
			float3(cosine, sine, 0),
			float3(-sine, cosine, 0),
			float3(0, 0, 1));

		vpos = mul(tilt, vpos);
		
		float3 worldPos = meshPositionWS + cameraX * vpos.x + cameraY * vpos.y;
		
		output.positionCS = TransformWorldToHClip(worldPos);
		output.positionWS = worldPos;
		
		float3 color = hsv2rgb(float3(data.w * 0.25, 1, 1));
		color = saturate(data.www);

		color = saturate(data.www);
		color.r = data.x;
		color.g = data.w;
		color.b = pheromone.life;
		
		output.color = float4(color, GetSize(pheromone));
		output.uv = input.uv;
		output.screenUV = (output.positionCS.xy / output.positionCS.w) * 0.5 + 0.5;
		output.instanceID = instance_id;


		float depth = -TransformWorldToView(worldPos).z;

		//depth = LinearDepthToNonLinear(output.positionCS.z / output.positionCS.w, _ZBufferParams);
		output.depth = depth;
		
		return output;
	}
	
	Interpolators DepthOnlyVertex(Attributes input, uint instance_id: SV_InstanceID)
	{
		Interpolators output;
	
		InitIndirectDrawArgs(0);
        uint instanceID = GetIndirectInstanceID(instance_id);
		uint index = instanceID;
		
		#if _SORT_ON
		SortEntry entry = _PheromoneSortingBuffer[instanceID];
		index = entry.index;
		#endif
		
		Pheromone pheromone = _PheromoneBuffer[index];

		float3 meshPositionWS = pheromone.pos;

		float3 vpos = input.positionOS.xyz * GetSize(pheromone);
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;

		float3 cameraZ = (meshPositionWS - GetCameraPositionWS());
		cameraX = normalize(cross(float3(0,1,0), cameraZ));
		cameraY = normalize(cross(cameraZ, cameraY));
		
		float3 worldPos = meshPositionWS + cameraX * vpos.x + cameraY * vpos.y;
		
		float4 outPos = TransformWorldToHClip(worldPos);
		
		output.positionCS = outPos;
		output.instanceID = instance_id;
	
		return output;
	}
	
	half DepthOnlyFragment(Interpolators input) : SV_TARGET
	{
		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
		return input.positionCS.z;
	}
    
	// The fragment function. This runs once per fragment, which you can think of as a pixel on the screen
	// It must output the final color of this pixel
	float4 Fragment(Interpolators input) : SV_TARGET{
		
		float2 uv = input.uv;

		float2 offset = uv * 2 - 1;
		float4 colorSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

		float2 screenUv = GetNormalizedScreenSpaceUV(input.positionCS);
		
		float sceneDepth = SampleSceneDepth(screenUv);
		//sceneDepth = SAMPLE_TEXTURE2D(_CameraDepthAttachment, sampler_LinearClamp, screenUv);

		//sceneDepth = LinearDepthToEyeDepth(sceneDepth);
		sceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);

		float zFade = saturate(_ZFade * (-input.depth + sceneDepth));
		
		if (dot(offset, offset) > 1)
			discard;

		float3 normal = 0;
		normal.xy = offset;
		normal.z = sqrt(1 - dot(offset, offset));


		float shadow = 1;
		float4 shadowCoords = TransformWorldToShadowCoord(input.positionWS);
		
		Light light = GetMainLight(shadowCoords);
		float3 lightDirection = TransformWorldToViewDir(light.direction);
		lightDirection = light.direction;
		float lightStrength = dot(normal, lightDirection) * 0.5 + 0.5;
		
		float thickness = normal.z * input.color.w * 2;
		
		float transmittance = 1;

		float3 samplePos = input.positionWS;
		float stepSize = 2;
		float lightDensity = 0;
		float smoothingRadius = 1;

		int steps = 0;

		if (input.instanceID % 4 == 0)
			steps = 1;
		
		for (int i = 0; i < steps; i++)
		{
			samplePos += lightDirection * stepSize;
			lightDensity += SampleDensityAtPoint(samplePos, smoothingRadius) * stepSize;
		}
		float lightTransmittance = exp(-lightDensity * _LightAbsorption);
		lightTransmittance *= shadow;

		shadow = light.shadowAttenuation;
		
		float3 normalWS = TransformViewToWorldDir(normal);


		float3 col = 0;
		col = input.color.rgb;
		col = 1;
		//col = hsv2rgb(float3(input.color.g, 1, 1));

		half4 pheromoneColor = lerp(_OffColor, _MainColor, input.color.g);
		
		//return thickness;

		//col.rgb *= colorSample.rgb;
		float alpha = saturate(normal.z * input.color.z * input.color.z);
		alpha *= colorSample.a;
		alpha *= pheromoneColor.a;

		alpha *= zFade;

		lightStrength *= shadow;

		transmittance = exp(-thickness * (1-alpha));

		lightStrength = lightTransmittance;
		
		//lightStrength = transmittance;
		
		//col = NumberToDots(input.color, uv);
		//col = 1;

		col *= pheromoneColor.rgb;
		col *= light.color;
		float3 gi = SAMPLE_GI(input.lightmapUV, 0, normalWS);
		//col = saturate(col + gi);
		float3 shadowColor = lerp(0, gi, saturate(1-normal.z));

		col = lerp(shadowColor, col, lightStrength);

		//col = transmittance;
		
		//col = lightStrength;
		
		//col = zFade;
		
		return float4(col, alpha);
		return float4(normal * 0.5 + 0.5, input.color.x);
		
		return input.color * colorSample;
		
		InputData lightingInput = (InputData)0; // Found in URP/ShaderLib/Input.hlsl
		lightingInput.positionWS = input.positionWS;
		lightingInput.normalWS = (normalWS);
		lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); // In ShaderVariablesFunctions.hlsl
		lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS); // In Shadows.hlsl
		lightingInput.bakedGI = SAMPLE_GI(input.lightmapUV, 0, normalWS);
		lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
		SurfaceData surfaceInput = (SurfaceData)0;
		surfaceInput.albedo = col;
		surfaceInput.alpha = alpha;
		surfaceInput.specular = 0;
		surfaceInput.smoothness = 1;
		surfaceInput.occlusion = 1;
		
		return UniversalFragmentPBR(lightingInput, surfaceInput);
	}

    
	// These are set by Unity for the light currently "rendering" this shadow caster pass
	float3 _LightDirection;
	
	// This function offsets the clip space position by the depth and normal shadow biases
	float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS) {
	    float3 lightDirectionWS = _LightDirection;
	    // From URP's ShadowCasterPass.hlsl
	    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
	    // We have to make sure that the shadow bias didn't push the shadow out of
	    // the camera's view area. This is slightly different depending on the graphics API
	    #if UNITY_REVERSED_Z
	    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
	    #else
	    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
	    #endif
	    positionCS = ApplyShadowClamping(positionCS);
	    return positionCS;
	}
	
	Interpolators ShadowVertex(Attributes input, uint instance_id: SV_InstanceID)
	{
	    Interpolators output;
	
		InitIndirectDrawArgs(0);
        uint instanceID = GetIndirectInstanceID(instance_id);
		uint index = instanceID;
		
		#if _SORT_ON
		SortEntry entry = _PheromoneSortingBuffer[instanceID];
		index = entry.index;
		#endif
		
		Pheromone pheromone = _PheromoneBuffer[index];
		
	    float3 meshPositionWS = pheromone.pos;
	    
		float3 vpos = input.positionOS.xyz * GetSize(pheromone);
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;

		float3 worldPos = meshPositionWS + cameraX * vpos.x + cameraY * vpos.y;
	    
	    output.positionCS = GetShadowCasterPositionCS(worldPos, input.normalOS);
	
	    return output;
	}
	
	
	float4 ShadowFragment(Interpolators input) : SV_TARGET {
	    return 0;
	}
	    		
	    
	
	    
    ENDHLSL
  
    SubShader
    {
    	Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}
    	
        Pass //Base with Ambient Light
    	{
    		Name "ForwardLit"
	        Tags { "LightMode" = "UniversalForward" "RenderType"="Transparent"}
	
	        ZWrite Off
    		Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
    		//BlendOp Add, Multiply
    		
    		Cull Back
    		ZTest LEqual
        	
        	HLSLPROGRAM

        	//#define _SPECULAR_COLOR
        	
        	#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

        	#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
        	#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

        	
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
    		#pragma vertex Vertex
			#pragma fragment Fragment
        	
        	ENDHLSL
		}
    	
    	Pass //Base with Ambient Light
    	{
    		Name "ForwardLit Stenci"
	        Tags { "LightMode" = "UniversalForwardStencil" "RenderType"="Transparent"}
	
	        ZWrite Off
    		Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
    		//BlendOp Add, Multiply
    		
    		Cull Back
    		ZTest LEqual
	
    		Stencil 
    		{
    			Ref 1
    			Comp NotEqual
            }	
        	
        	HLSLPROGRAM

        	//#define _SPECULAR_COLOR
        	
        	#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

        	#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
        	#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

        	
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
    		#pragma vertex Vertex
			#pragma fragment Fragment
        	
        	ENDHLSL
		}
    	
    	
        Pass //Shadow Caster
    	{
    		Name "ShadowCaster"
    		Tags { "LightMode" = "ShadowCaster"}	

    		ColorMask 0
    		HLSLPROGRAM

    		#pragma vertex ShadowVertex
			#pragma fragment ShadowFragment

    		ENDHLSL
        }
    	
    	
    	Pass
        {
            Name "DepthOnly"
            Tags
            { "LightMode" = "DepthOnly"  "RenderType"="Opaque"}

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Includes
            
            ENDHLSL
        }
    }
    Fallback "VertexLit"
}