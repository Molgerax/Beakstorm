Shader "DynaMak/Particles/Procedural Billboard"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    
    CGINCLUDE

    #include "UnityCG.cginc"
	#include "ParticleRenderUtility.hlsl"
    
    
    #pragma target 3.5
    #pragma shader_feature _ _SHADOWMODE_ON

    
    
    struct v2f
    {
        float4 clipPos			: SV_POSITION;
		fixed4 color			: COLOR;
		float3 normal			: TEXCOORD0;
        
        #if _SHADOWMODE_ON
			float4 shadowCoord	: COLOR1;
		#endif
    };
    
    //Properties
    uniform float4 _LightColor0;
    
    fixed4 _Color;
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


    
    v2f vert(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
    {
        v2f o;
    	
        RenderTri tri = _TriangleBufferShader[instance_id];
    	RenderVertex v = tri.v[0];
        float3 worldPos = v.positionWS;

    	o.color = v.color;
    	o.normal = v.normalWS;

    	#if _SHADOWMODE_ON
		o.clipPos = UnityWorldToClipPos(float4(worldPos - o.normal * 0.0, 1));
    	#else
        o.clipPos = UnityWorldToClipPos(float4(worldPos, 1));
    	#endif

    	
        #if _SHADOWMODE_ON
			o.shadowCoord = ProjectionToTextureSpace(o.clipPos);
		#endif
        
        return o;
    }

    fixed4 frag(v2f i) : SV_TARGET
    {
        fixed3 col = i.color.rgb * _Color.rgb;

    	
        float3 vertexToLight = _WorldSpaceLightPos0.xyz;
		float vertexToLightDist = length(vertexToLight);
		float attenuation = lerp(1, 1 / vertexToLightDist, _WorldSpaceLightPos0.w);

		float3 lightDir = lerp(_WorldSpaceLightPos0.xyz, vertexToLight, _WorldSpaceLightPos0.w);
		

		float3 diffRefl = attenuation * _LightColor0.rgb * col.rgb * max(0, dot(i.normal, lightDir));

		#if _SHADOWMODE_ON
			diffRefl.rgb *= tex2D(_ShadowMapTexture, i.shadowCoord).a;
		#endif

    	
        return fixed4(diffRefl, 1);
    }

    fixed4 fragAmbient(v2f i) : SV_TARGET
    {
	    fixed3 col = i.color.rgb * _Color.rgb;
    	
	    float3 vertexToLight = _WorldSpaceLightPos0.xyz;
	    float vertexToLightDist = length(vertexToLight);
	    float attenuation = lerp(1, 1 / vertexToLightDist, _WorldSpaceLightPos0.w);

	    float3 lightDir = lerp(_WorldSpaceLightPos0.xyz, vertexToLight, _WorldSpaceLightPos0.w);
		

	    float3 diffRefl = attenuation * _LightColor0.rgb * col.rgb * max(0, dot(i.normal, lightDir));
	    float3 ambientLight = UNITY_LIGHTMODEL_AMBIENT.rgb * col.rgb;

	    #if _SHADOWMODE_ON
    	
    		float closestDepth = tex2D(_ShadowMapTexture, i.shadowCoord).a;
    	
    		float bias = max(0.05 * (1.0 - dot(i.normal, lightDir)), 0.005);
    		float shadow =  closestDepth;
			diffRefl.rgb *= shadow;
	    #endif

    	
	    return fixed4(ambientLight + diffRefl, 1);
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
			#pragma fragment frag
        	
        	ENDCG
		}
    	
    	Pass //Shadow Caster
    	{
    		Name "ShadowCaster"
    		Tags { "Queue" = "Opaque" "LightMode" = "ShadowCaster" }	
    		Zwrite On
    		Cull Off
    		
    		CGPROGRAM

    		#pragma vertex vert
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