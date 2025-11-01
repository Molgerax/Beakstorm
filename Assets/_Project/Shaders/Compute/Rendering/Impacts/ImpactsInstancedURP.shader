Shader "BeakStorm/Impacts/Instanced URP"
{
    Properties
    {
        _MainColor("Main Color", Color) = (1, 1, 1, 1)
        _OffColor("Off Color", Color) = (1, 1, 1, 1)
    	_MainTex("Texture", 2D) = "white" {}
    	_VertexColorToBase("Vertex Color Mask for Base", Range(0.0,1.0)) = 1
    	
        [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
    	
    	[HDR] _EmissiveColor("Emission Color", Color) = (0, 0, 0, 0)
        [NoScaleOffset] _EmissiveMap("Emission Map", 2D) = "white" {}
    	_VertexColorToEmissive("Vertex Color Mask for Emission", Range(0.0,1.0)) = 1
    	_Size("Size", Float) = 1
    	
    	_UvOffset("UV Offset", Range(-0.5,0.5)) = 0
    }
    
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
    #include "UnityIndirect.cginc"

	#include "../../Collisions/ImpactUtility.hlsl"
    
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

    TEXTURE2D(_SpriteSheet);
    //CBUFFER_START(UnityPerObject)
	//CBUFFER_END
    
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

    float _UvOffset;
    
	int _SpriteCount;
    int _SpriteFrameCount;
    int _SpriteHeight;
    
    StructuredBuffer<Impact> _ImpactBuffer;
 
    CBUFFER_END

    
	
	    // Pull in URP library functions and our own common functions
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

	// Textures
	SAMPLER(sampler_MainTex);
	SAMPLER(sampler_SpriteSheet);
	
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


    float2 GetSpriteUV(float2 uv, int spriteId, int frame)
	{
		float height = 1.0 / _SpriteCount;
		uv.x /= _SpriteFrameCount;
		uv.y /= _SpriteCount;

		uv.x += (1.0 / _SpriteFrameCount) * frame;
		uv.y += height * spriteId;
		return uv;
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

    float GetSize(Impact i)
	{
		uint damage = GetDamage(i);
		float dmg = lerp(0.1, 1, damage/10.0);
		
		float maxTime = GetMaxTime(i);
		float t = saturate(i.time / maxTime);

		float ot = 1 - (1-t) * 0.5;

		float f = ot;
		float g = 8 * ot * ot *ot *ot;

		t = lerp(f, g, 1-ot);

		t = (t * 2 - 1);

		//t = 1;
		
		return t * _Size * dmg;
	}

    float Dither(float In, float2 ScreenPosition)
	{
		float2 uv = ScreenPosition.xy * _ScreenParams.xy;
		const float DITHER_THRESHOLDS[16] =
		{
			1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
			13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
			4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
			16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
		};
		uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
		return In - DITHER_THRESHOLDS[index];
	}


    
    
	Interpolators Vertex(Attributes input, uint instance_id: SV_InstanceID)
	{
		Interpolators output = (Interpolators)0;

		InitIndirectDrawArgs(0);
        uint instanceID = GetIndirectInstanceID(instance_id);
		uint index = instanceID;
		
		Impact impact = _ImpactBuffer[index];
		if (impact.time <= 0)
			return (Interpolators)(1.0 / 0.0);
		
		float3 meshPositionWS = impact.position;
		uint matIndex = GetMaterialIndex(impact);
		uint damage = GetDamage(impact);

		float data = damage / 10.0;

		float3 offset = float3(_UvOffset,0,0);
		float3 vpos = input.positionOS.xyz + offset;
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;
		float3 cameraZ = unity_CameraToWorld._m02_m12_m22;

		float3 cameraDiff = GetCameraPositionWS() - impact.position;
		
		float3 up = impact.normal;
		float3 right = cross(up, cameraDiff);
		//if (dot(right, right) == 0)
		{
			up = cameraY;
			right = cameraX;
		}
		float3 fwd = cross(up, right);

		float random = GenerateHashedRandomFloat(asuint(impact.position.x) + asuint(impact.position.y) + asuint(impact.position.z));
		
		float angle = (random * 2 - 1) * 6.28;
		float sine, cosine;
		sincos(angle, sine, cosine);
		
		float3x3 angleRot = float3x3(
			float3(cosine, sine, 0),
			float3(-sine, cosine, 0),
			float3(0, 0, 1));

		vpos = mul(angleRot, vpos);
		
		float3x3 tilt = float3x3(
			normalize(up),
			normalize(right),
			normalize(fwd));

		tilt = transpose(tilt);
		vpos = mul(tilt, vpos);
		
		float3 worldPos = meshPositionWS + vpos * GetSize(impact);

		output.positionCS = TransformWorldToHClip(worldPos);

		output.positionCS.z += 0.01;
		
		float3 color = hsv2rgb(float3(data * 0.25, 1, 1));
		color = saturate(data);

		float maxTime = GetMaxTime(impact);
		float t = 1 - (impact.time / maxTime);

		int frame = floor(t * _SpriteFrameCount);

		float2 uv = GetSpriteUV(input.uv, matIndex - 1, frame);
		//uv = input.uv;
		
		output.color = float4(color, GetSize(impact));
		output.uv = uv;
		output.screenUV = GetNormalizedScreenSpaceUV(output.positionCS);
	
		output.instanceID = instance_id;
		
		output.positionWS = worldPos;
	
		return output;
	}
	
	Interpolators DepthOnlyVertex(Attributes input, uint instance_id: SV_InstanceID)
	{
		Interpolators output;
	
		InitIndirectDrawArgs(0);
        uint instanceID = GetIndirectInstanceID(instance_id);
		uint index = instanceID;
		
		
		Impact impact = _ImpactBuffer[index];
		if (impact.time <= 0)
			return (Interpolators)(1.0 / 0.0);
		
		float3 meshPositionWS = impact.position;
		uint matIndex = GetMaterialIndex(impact);
		uint damage = GetDamage(impact);

		float data = damage / 10.0;
		
		float3 vpos = input.positionOS.xyz + float3(_UvOffset,0,0);
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;
		float3 cameraZ = unity_CameraToWorld._m02_m12_m22;

		float3 up = impact.normal;
		float3 right = cross(up, cameraZ);
		if (dot(right, right) == 0)
		{
			up = cameraY;
			right = cameraX;
		}
		float3 fwd = cross(up, right);
		
		float3x3 tilt = float3x3(
			normalize(right),
			normalize(up),
			normalize(fwd));

		vpos = mul(tilt, vpos);
		
		float3 worldPos = meshPositionWS + vpos * GetSize(impact);
		
		float4 outPos = TransformWorldToHClip(worldPos);

		output.positionWS = worldPos;
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
		float4 colorSample = SAMPLE_TEXTURE2D(_SpriteSheet, sampler_SpriteSheet, uv);

		
		
		float3 cameraDiff = GetCameraPositionWS() - input.positionWS;
		float distanceToCam = length(cameraDiff);
		distanceToCam = saturate(distanceToCam / 10);
		float dither = Dither(distanceToCam, GetNormalizedScreenSpaceUV(input.positionCS.xy));
		clip(dither);

		
		if (colorSample.a < 0.1)
			discard;

		float3 normal = 0;
		normal.xy = offset;
		normal.z = sqrt(1 - dot(offset, offset));

		float3 normalWS = TransformViewToWorldDir(normal);


		float3 col = 0;
		col = input.color.rgb;
		col = 1;

		half4 impactColor = lerp(_OffColor, _MainColor, input.color.r);
		
		//col *= impactColor.rgb;
		col *= colorSample.rgb;
		return float4(col, 1);
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
		surfaceInput.alpha = 1;
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
		
		Impact impact = _ImpactBuffer[index];
		if (impact.time <= 0)
			return (Interpolators)(1.0 / 0.0);
		
		float3 meshPositionWS = impact.position;
		uint matIndex = GetMaterialIndex(impact);
		uint damage = GetDamage(impact);

		float data = damage / 10.0;
		
		float3 vpos = input.positionOS.xyz + float3(_UvOffset,0,0);
		float3 cameraX = unity_CameraToWorld._m00_m10_m20;
		float3 cameraY = unity_CameraToWorld._m01_m11_m21;
		float3 cameraZ = unity_CameraToWorld._m02_m12_m22;

		float3 up = impact.normal;
		float3 right = cross(up, cameraZ);
		if (dot(right, right) == 0)
		{
			up = cameraY;
			right = cameraX;
		}
		float3 fwd = cross(up, right);
		
		float3x3 tilt = float3x3(
			normalize(right),
			normalize(up),
			normalize(fwd));

		vpos = mul(tilt, vpos);
		
		float3 worldPos = meshPositionWS + vpos * GetSize(impact);
	    
	    output.positionCS = GetShadowCasterPositionCS(worldPos, input.normalOS);
	
	    return output;
	}
	
	
	float4 ShadowFragment(Interpolators input) : SV_TARGET {
	    return 0;
	}
	    		
	    
	
	    
    ENDHLSL
  
    SubShader
    {
    	Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Overlay"}
    	
        Pass //Base with Ambient Light
    	{
    		Name "ForwardLit"
	        Tags { "LightMode" = "UniversalForward" "RenderType"="Opaque"}
	
	        ZWrite On
    		//Blend SrcAlpha OneMinusSrcAlpha
    		Cull Off
	
        	
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