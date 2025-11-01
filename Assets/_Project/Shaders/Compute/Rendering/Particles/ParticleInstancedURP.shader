Shader "BeakStorm/Particles/Instanced URP"
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
    
    StructuredBuffer<float3> _PositionBuffer;
    StructuredBuffer<float3> _OldPositionBuffer;
    StructuredBuffer<float3> _VelocityBuffer;
    StructuredBuffer<float3> _NormalBuffer;
    StructuredBuffer<float4> _DataBuffer;

    CBUFFER_END
    ENDHLSL
  
    SubShader
    {
    	Tags { "RenderPipeline" = "UniversalPipeline" }
    	
        Pass //Base with Ambient Light
    	{
    		Name "ForwardLit"
	        Tags { "LightMode" = "UniversalForward" "RenderType"="Opaque"}
	
	        ZWrite On
    		Cull Back
	
        	
        	HLSLPROGRAM

        	#define _SPECULAR_COLOR
        	
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
        	
    		#include "ParticleForwardLitPass.hlsl"
        	
        	ENDHLSL
		}
    	
    	
        Pass //Shadow Caster
    	{
    		Name "ShadowCaster"
    		Tags { "LightMode" = "ShadowCaster"}	

    		ColorMask 0
    		HLSLPROGRAM

    		#pragma vertex Vertex
			#pragma fragment Fragment
        	
    		#include "ParticleForwardShadowCasterPass.hlsl"

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
    		#include "ParticleForwardLitPass.hlsl"
            ENDHLSL
        }
    }
    Fallback "VertexLit"
}