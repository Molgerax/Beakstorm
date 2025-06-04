Shader "BeakStorm/Pheromones/Grid Instanced URP"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    	_MainTex("Texture", 2D) = "white" {}
    	_VertexColorToBase("Vertex Color Mask for Base", Range(0.0,1.0)) = 1
    	
        [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
    	
    	[HDR] _EmissiveColor("Emission Color", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _EmissiveMap("Emission Map", 2D) = "white" {}
    	_VertexColorToEmissive("Vertex Color Mask for Emission", Range(0.0,1.0)) = 1
    	
    }
    
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
    #include "UnityIndirect.cginc"
    
    #pragma target 3.5
    #pragma shader_feature _ _SHADOWMODE_ON
	#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
    #pragma multi_compile_fwdadd_fullshadows
    
    
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

    
    half4 _Color;
    float _VertexColorToBase;
    float3 _EmissiveColor;
	float _VertexColorToEmissive;

    
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
 
    CBUFFER_END

    
	
	    // Pull in URP library functions and our own common functions
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	
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
	};
	
	float3 hsv2rgb(float3 hsv)
	{
		float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
		float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
	
		return hsv.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);
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

    
	Interpolators Vertex(Attributes input, uint instance_id: SV_InstanceID)
	{
		Interpolators output = (Interpolators)0;

		InitIndirectDrawArgs(0);
        uint instanceID = GetIndirectInstanceID(instance_id);
		SortEntry entry = _PheromoneSortingBuffer[instanceID];
		Pheromone pheromone = _PheromoneBuffer[entry.index];
		
		if (pheromone.life <= 0)
			return (Interpolators)(1.0 / 0.0);
		
		float3 meshPositionWS = pheromone.pos;
		float4 data = pheromone.data;
		

		float3 vpos = input.positionOS.xyz * saturate(data.w) * 0.25;
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;

		float3 worldPos = meshPositionWS + cameraX * vpos.x + cameraY * vpos.y;
		
		output.positionCS = TransformWorldToHClip(worldPos);
		
		float3 color = hsv2rgb(float3(data.w * 0.25, 1, 1));
		color = saturate(data.www);

		color.x = frac(data.w);
		color.y = saturate((data.w - frac(data.w)) / 4);
		color.z = 0;

		color = hsv2rgb(float3(data.w, 1, 1));
		color = saturate(data.www);
		color = saturate(entry.dist);
		
		output.color = float4(color, data.w);
		output.uv = input.uv;
		output.screenUV = GetNormalizedScreenSpaceUV(output.positionCS);
	
		output.instanceID = instance_id;
		
	
		return output;
	}
	
	Interpolators DepthOnlyVertex(Attributes input, uint instance_id: SV_InstanceID)
	{
		Interpolators output;
	
		InitIndirectDrawArgs(0);
        uint instanceID = GetIndirectInstanceID(instance_id);
		SortEntry entry = _PheromoneSortingBuffer[instanceID];
		Pheromone pheromone = _PheromoneBuffer[entry.index];

		float3 meshPositionWS = pheromone.pos;

		float3 vpos = input.positionOS.xyz * 0.5;
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;

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

		if (dot(offset, offset) > 1)
			discard;

		float3 normal = 0;
		normal.xy = offset;
		normal.z = sqrt(1 - dot(offset, offset));

		float thickness = normal.z * 0.01;
		
		float3 normalWS = TransformViewToWorldDir(normal);


		float3 col = 0;
		col.x = frac(input.color.x);
		col.y = frac(input.color.x * 2);
		col.z = frac(input.color.x * 4);

		col = hsv2rgb(float3(input.color.x, 1, 1));
		col = input.color.rgb;
		//return thickness;

		//col = NumberToDots(input.color, uv);
		
		return float4(col, input.color.w);
		return float4(normal * 0.5 + 0.5, input.color.x);
		
		return input.color * colorSample;
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
		SortEntry entry = _PheromoneSortingBuffer[instanceID];
		Pheromone pheromone = _PheromoneBuffer[entry.index];
		
	    float3 meshPositionWS = pheromone.pos;
	    
		float3 vpos = input.positionOS.xyz;
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
    		Blend SrcAlpha OneMinusSrcAlpha
    		Cull Back
	
        	
        	HLSLPROGRAM

        	//#define _SPECULAR_COLOR
        	
        	//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
//
        	//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            //#pragma multi_compile_fragment _ _LIGHT_COOKIES
        	//#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            //#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            //#pragma multi_compile _ _LIGHT_LAYERS
            //#pragma multi_compile _ _FORWARD_PLUS
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