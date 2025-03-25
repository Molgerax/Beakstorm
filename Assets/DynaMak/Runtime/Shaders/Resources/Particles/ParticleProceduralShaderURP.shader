Shader "DynaMak/Particles/Procedural URP"
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

    //#include "AutoLight.cginc"
    //#include "Lighting.cginc"
    
	#include "ParticleRenderUtility.hlsl"
    
    
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
    
    StructuredBuffer<RenderTri> _TriangleBufferShader;

    CBUFFER_END
    
    // FUNCTIONS
    inline float4 ProjectionToTextureSpace(float4 pos)
	{
		float4 textureSpacePos = pos;
		#if defined(UNITY_HALF_TEXEL_OFFSET)
			textureSpacePos.xy = float2(textureSpacePos.x, textureSpacePos.y * _ProjectionParams.x) + textureSpacePos.w * _ScreenParams.zw;
		#else
			textureSpacePos.xy = float2(textureSpacePos.x, textureSpacePos.y * _ProjectionParams.x) + textureSpacePos.w;
		#endif
			textureSpacePos.xy = float2(textureSpacePos.x / textureSpacePos.w, textureSpacePos.y / textureSpacePos.w) * 0.5;
		return textureSpacePos;
	}

    
/*
    half4 fragAmbient(v2f i) : SV_TARGET
    {
	    float4 texSample = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);

        half3 col = texSample.rgb * _Color.rgb * lerp(1, i.color.rgb, _VertexColorToBase);
    	float alpha = texSample.a * i.color.a;

    	float3 normalMap = normalize(UnpackNormal(tex2D(_NormalMap, i.uv * _MainTex_ST.xy + _MainTex_ST.zw)));
		float3 normal = BlendNormals(i.normal, normalMap);
    	
	    float3 vertexToLight = _WorldSpaceLightPos0.xyz - i.worldPos;
	    float vertexToLightDist = length(vertexToLight);
	    float attenuation = lerp(1, 1 / vertexToLightDist, _WorldSpaceLightPos0.w);

	    float3 lightDir = lerp(_WorldSpaceLightPos0.xyz, vertexToLight, _WorldSpaceLightPos0.w);
    	
    	UNITY_LIGHT_ATTENUATION(lightAttenuation, i, i.worldPos)

	    float3 diffRefl = attenuation * _LightColor0.rgb * col.rgb * max(0, dot(normal, lightDir)) * lightAttenuation;
	    float3 ambientLight = ShadeSH9(half4(normal, 1) ) * col.rgb;

    	fixed3 emissiveMap = tex2D(_EmissiveMap, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
    	float3 emissive = emissiveMap * _EmissiveColor * lerp(1, i.color, _VertexColorToEmissive);

	    #if _SHADOWMODE_ON
    	
    		float closestDepth = tex2D(_ShadowMapTexture, i.shadowCoord).a;
    	
    		float bias = max(0.05 * (1.0 - dot(i.normal, lightDir)), 0.005);
    		float shadow =  closestDepth;
			diffRefl.rgb *= shadow;
	    #endif
    	
	    return fixed4(ambientLight + diffRefl + emissive, alpha);
    }

    fixed4 fragAdd(v2f i) : SV_TARGET
    {
    	float4 texSample = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);

        fixed3 col = texSample.rgb * _Color.rgb * lerp(1, i.color.rgb, _VertexColorToBase);
    	float alpha = texSample.a * i.color.a;

    	float3 normalMap = normalize(UnpackNormal(tex2D(_NormalMap, i.uv * _MainTex_ST.xy + _MainTex_ST.zw)));
		float3 normal = BlendNormals(i.normal, normalMap);

    	
        float3 vertexToLight = _WorldSpaceLightPos0.xyz - i.worldPos.xyz;
		float vertexToLightDist = length(vertexToLight);
		float attenuation = lerp(1, 1 / vertexToLightDist, _WorldSpaceLightPos0.w);

		float3 lightDir = lerp(_WorldSpaceLightPos0.xyz, vertexToLight, _WorldSpaceLightPos0.w);
		

		float3 diffRefl = attenuation * _LightColor0.rgb * col.rgb * max(0, dot(normal, lightDir)) * lightAttenuation;

		#if _SHADOWMODE_ON
			diffRefl.rgb *= tex2D(_ShadowMapTexture, i.shadowCoord).a;
		#endif

        return float4(diffRefl, alpha);
    }
*/
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
        	
        	#pragma shader_feature_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma shader_feature_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
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
    	/*
    	Pass //Base without Ambient Light
    	{
	        Tags { "LightMode" = "ForwardAdd" "RenderType"="Opaque" "Queue" = "Geometry"}
        	Blend One One
	
        	
        	HLSLPROGRAM
	
        	#pragma vertex vert
			#pragma fragment fragAdd
        	
        	ENDHLSL
		}
    	*/
    	
    	
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