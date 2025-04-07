Shader "DynaMak/Particles/Procedural"
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
    
    CGINCLUDE

    #include "UnityCG.cginc"
    #include "AutoLight.cginc"
    #include "Lighting.cginc"
    
	#include "ParticleRenderUtility.hlsl"
    
    
    #pragma target 3.5
    #pragma shader_feature _ _SHADOWMODE_ON
	#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
    #pragma multi_compile_fwdadd_fullshadows
    
    
    struct v2f
    {
        float4 pos				: SV_POSITION;
		fixed4 color			: COLOR;
    	float2 uv				: TEXCOORD0;
		float3 normal			: TEXCOORD1;
    	float4 tangent			: TEXCOORD2;
    	float3 worldPos			: TEXCOORD3;
    	LIGHTING_COORDS(4,5)
    	
    	
        #if _SHADOWMODE_ON
			float4 shadowCoord	: COLOR1;
		#endif
    };
    
    //Properties

    sampler2D _MainTex;
    float4 _MainTex_ST;
    sampler2D _NormalMap;
	sampler2D _EmissiveMap;

    int _SubMeshId;
    
    fixed4 _Color;
    float _VertexColorToBase;
    float3 _EmissiveColor;
	float _VertexColorToEmissive;
    
    StructuredBuffer<RenderTri> _TriangleBufferShader;

    #if _SHADOWMODE_ON
		sampler2D _ShadowMapTexture;
    #endif

    
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

    float4 UnityWorldSpaceShadowCasterPos(float3 worldPos, float3 normal, float strength = 1)
	{
    	float4 wPos = float4(worldPos,1);
	    if (unity_LightShadowBias.z != 0.0)
	    {
	        float3 wNormal = UnityObjectToWorldNormal(normal);
	        float3 wLight = normalize(UnityWorldSpaceLightDir(wPos.xyz));
	
	        // apply normal offset bias (inset position along the normal)
	        // bias needs to be scaled by sine between normal and light direction
	        // (http://the-witness.net/news/2013/09/shadow-mapping-summary-part-1/)
	        //
	        // unity_LightShadowBias.z contains user-specified normal offset amount
	        // scaled by world space texel size.
	
	        float shadowCos = dot(wNormal, wLight);
	        float shadowSine = sqrt(1-shadowCos*shadowCos);
	        float normalBias = unity_LightShadowBias.z * shadowSine;
	
	        wPos.xyz -= wNormal * normalBias * strength;
	    }
	
	    return mul(UNITY_MATRIX_VP, wPos);
	}


    
    v2f vert(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
    {
        v2f o;
    	
        RenderTri tri = _TriangleBufferShader[instance_id];
    	RenderVertex vert = tri.v[2 - vertex_id];
        float3 worldPos = vert.positionWS;

    	o.color = vert.color;
    	o.normal = vert.normalWS;
    	o.tangent = vert.tangentWS;
    	o.uv = vert.uv;
    	o.worldPos = worldPos;


    	#if _SHADOWMODE_ON
		o.clipPos = UnityWorldToClipPos(float4(worldPos - o.normal * 0.0, 1));
    	#else
        o.pos = UnityWorldToClipPos(float4(worldPos, 1));
    	#endif

    	
        #if _SHADOWMODE_ON
			o.shadowCoord = ProjectionToTextureSpace(o.clipPos);
		#endif

		TRANSFER_SHADOW_WPOS(o, o.worldPos)
    	
    	if(tri.subMeshId != _SubMeshId)
    		o.pos.w = 0.0 / 0.0;
    	
        return o;
    }

    v2f vertShadow(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
    {
        v2f o;
    	
        RenderTri tri = _TriangleBufferShader[instance_id];
    	RenderVertex vert = tri.v[2 - vertex_id];
        float3 worldPos = vert.positionWS;

    	o.color = vert.color;
    	o.normal = vert.normalWS;
    	o.tangent = vert.tangentWS;
    	o.uv = vert.uv;
    	o.worldPos = worldPos;

    	o.pos = UnityWorldSpaceShadowCasterPos(o.worldPos, o.normal, 1);
    	o.pos = UnityApplyLinearShadowBias(o.pos);

    	
        #if _SHADOWMODE_ON
			o.shadowCoord = ProjectionToTextureSpace(o.clipPos);
		#endif

		TRANSFER_SHADOW_WPOS(o, o.worldPos)

    	if(tri.subMeshId != _SubMeshId)
    		o.pos.w = 0.0 / 0.0;
    	
        return o;
    }
    

    fixed4 fragAmbient(v2f i) : SV_TARGET
    {
	    float4 texSample = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);

        fixed3 col = texSample.rgb * _Color.rgb * lerp(1, i.color.rgb, _VertexColorToBase);
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
		
    	UNITY_LIGHT_ATTENUATION(lightAttenuation, i, i.worldPos)

		float3 diffRefl = attenuation * _LightColor0.rgb * col.rgb * max(0, dot(normal, lightDir)) * lightAttenuation;

		#if _SHADOWMODE_ON
			diffRefl.rgb *= tex2D(_ShadowMapTexture, i.shadowCoord).a;
		#endif

        return float4(diffRefl, alpha);
    }

    ENDCG
  
    SubShader
    {
        Pass //Base with Ambient Light
    	{
	        Tags { "LightMode" = "ForwardBase" "RenderType"="Opaque" "Queue" = "Geometry"}
	
	        ZWrite On
    		Cull Back
	
        	
        	CGPROGRAM
	
        	#pragma vertex vert
			#pragma fragment fragAmbient
        	
        	ENDCG
		}
    	
    	Pass //Base without Ambient Light
    	{
	        Tags { "LightMode" = "ForwardAdd" "RenderType"="Opaque" "Queue" = "Geometry"}
        	Blend One One
	
        	
        	CGPROGRAM
	
        	#pragma vertex vert
			#pragma fragment fragAdd
        	
        	ENDCG
		}
    	
    	Pass //Shadow Caster
    	{
    		Name "ShadowCaster"
    		Tags { "Queue" = "Transparent" "LightMode" = "ShadowCaster" }	
    		Zwrite On
    		Cull Off
    		
    		CGPROGRAM

    		#pragma vertex vertShadow
			#pragma fragment fragShadow
        	

    		float4 fragShadow(v2f i) : SV_Target
    		{
				return 0;
    		}
    		ENDCG
        }
    }
    Fallback "VertexLit"
}