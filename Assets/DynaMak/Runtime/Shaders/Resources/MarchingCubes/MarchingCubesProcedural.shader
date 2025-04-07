Shader "DynaMak/Procedural/MarchingCubes"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    	_MainTex("Albedo", 2D) = "white" {}
        [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
    	
    	_Scrolling("Scrolling", Vector) = (0,0,0,0)
    	_FlatShading("Flat Shading", Range(0, 1)) = 0
    }
    
    CGINCLUDE

    #include "UnityCG.cginc"
    #include "AutoLight.cginc"
    #include "Lighting.cginc"

    #include "../TriplanarMapping.cginc"

    #pragma target 3.5
    #pragma shader_feature _ _SHADOWMODE_ON
	#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
    #pragma multi_compile_fwdadd_fullshadows
    
    
    //Structs
    struct Triangle
    {
        float3 v[3];
    	float3 n[3];
    };

    struct v2f
    {
        float4 pos				: SV_POSITION;
		fixed4 color			: COLOR;
		float3 normal			: TEXCOORD0;
    	float3 worldPos			: TEXCOORD1;
    	LIGHTING_COORDS(2,3)
        
        #if _SHADOWMODE_ON
			float4 shadowCoord	: COLOR1;
		#endif
    };
    
    //Properties
    sampler2D _MainTex;
    float4 _MainTex_ST;
    sampler2D _NormalMap;
	float4 _Scrolling;
    
    fixed4 _Color;
    float _FlatShading;
    StructuredBuffer<Triangle> _TriangleBuffer;

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
    	
        Triangle tri = _TriangleBuffer[instance_id];
        float3 worldPos = tri.v[2 - vertex_id];

    	o.worldPos = worldPos;
    	o.color = fixed4(1,1,1,1);
    	o.normal = normalize(cross(tri.v[2] - tri.v[0], tri.v[1] - tri.v[0]));
    	o.normal = lerp(tri.n[2 - vertex_id], o.normal, _FlatShading);

    	#if _SHADOWMODE_ON
		o.pos = UnityWorldToClipPos(float4(worldPos + o.normal * 0.01, 1));
    	#else
        o.pos = UnityWorldToClipPos(float4(worldPos, 1));
    	#endif

    	
        #if _SHADOWMODE_ON
			o.shadowCoord = ProjectionToTextureSpace(o.clipPos);
		#endif

		TRANSFER_SHADOW_WPOS(o, o.worldPos)
        return o;
    }

    v2f vertShadow(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
    {
        v2f o;
    	
        Triangle tri = _TriangleBuffer[instance_id];
        float3 worldPos = tri.v[2 - vertex_id];

    	o.worldPos = worldPos;
    	o.color = fixed4(1,1,1,1);
    	o.normal = normalize(cross(tri.v[2] - tri.v[0], tri.v[1] - tri.v[0]));
    	o.normal = lerp(tri.n[2 - vertex_id], o.normal, _FlatShading);

    	o.pos = UnityWorldSpaceShadowCasterPos(o.worldPos, o.normal, 5);
    	o.pos = UnityApplyLinearShadowBias(o.pos);
    	
        #if _SHADOWMODE_ON
			o.shadowCoord = ProjectionToTextureSpace(o.clipPos);
    	#endif

		TRANSFER_SHADOW_WPOS(o, o.worldPos)
        return o;
    }


    fixed4 fragAmbient(v2f i) : SV_TARGET
    {
    	float4 scalingOffset = _MainTex_ST;
    	float3 pos = i.worldPos + _Scrolling.xyz * _Scrolling.www * _Time.yyy;
    	fixed4 albedo = SampleTextureTriplanar(_MainTex, pos, i.normal, _MainTex_ST);
    	fixed3 col = i.color.rgb * _Color.rgb * albedo;

    	float3 normal = SampleNormalMapTriplanar(_NormalMap, pos, i.normal, _MainTex_ST);
    	
	    float3 vertexToLight = _WorldSpaceLightPos0.xyz - i.worldPos;
	    float vertexToLightDist = length(vertexToLight);
	    float attenuation = lerp(1, 1 / vertexToLightDist, _WorldSpaceLightPos0.w);

	    float3 lightDir = lerp(_WorldSpaceLightPos0.xyz, vertexToLight, _WorldSpaceLightPos0.w);

    	UNITY_LIGHT_ATTENUATION(lightAttenuation, i, i.worldPos)

	    float3 diffRefl = lightAttenuation * attenuation * _LightColor0.rgb * col.rgb * max(0, dot(normal, lightDir));
	    float3 ambientLight = UNITY_LIGHTMODEL_AMBIENT.rgb * col.rgb;

	    #if _SHADOWMODE_ON
    	
    		float closestDepth = tex2D(_ShadowMapTexture, i.shadowCoord).a;
    	
    		float bias = max(0.05 * (1.0 - dot(i.normal, lightDir)), 0.005);
    		float shadow =  closestDepth;
			diffRefl.rgb *= shadow;
	    #endif

    	
	    return fixed4(ambientLight + diffRefl, 1);
    }

    fixed4 fragAdd(v2f i) : SV_TARGET
    {
    	float4 scalingOffset = _MainTex_ST;
    	float3 pos = i.worldPos + _Scrolling.xyz * _Scrolling.www * _Time.yyy;
    	fixed4 albedo = SampleTextureTriplanar(_MainTex, pos, i.normal, _MainTex_ST);
    	fixed3 col = i.color.rgb * _Color.rgb * albedo;

    	float3 normal = SampleNormalMapTriplanar(_NormalMap, pos, i.normal, _MainTex_ST);
    	
        float3 vertexToLight = _WorldSpaceLightPos0.xyz  - i.worldPos.xyz;
		float vertexToLightDist = length(vertexToLight);
		float attenuation = lerp(1, 1 / vertexToLightDist, _WorldSpaceLightPos0.w);

		float3 lightDir = lerp(_WorldSpaceLightPos0.xyz, vertexToLight, _WorldSpaceLightPos0.w);

    	UNITY_LIGHT_ATTENUATION(lightAttenuation, i, i.worldPos)

		float3 diffRefl = lightAttenuation * attenuation * _LightColor0.rgb * col.rgb * max(0, dot(normal, lightDir));

		#if _SHADOWMODE_ON
			diffRefl.rgb *= tex2D(_ShadowMapTexture, i.shadowCoord).a;
		#endif

    	
        return fixed4(diffRefl, 1);
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
    		Tags { "Queue" = "Opaque" "LightMode" = "ShadowCaster" }	
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