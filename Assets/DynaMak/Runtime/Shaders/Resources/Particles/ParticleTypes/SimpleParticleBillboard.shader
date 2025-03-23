Shader "DynaMak/Particles/Billboard"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
	}
	
	CGINCLUDE


	#ifndef PARTICLE_SHADER
	#define PARTICLE_SHADER
	#endif
	

	#include "UnityCG.cginc"	
	#include "SimpleParticleSystem.compute"

	#pragma instancing_options procedural:setup
	#pragma target 3.5

	#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_SWITCH) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC)))
	#define SUPPORT_STRUCTUREDBUFFER
	#endif

	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && defined(SUPPORT_STRUCTUREDBUFFER)
	#define ENABLE_INSTANCING
	#endif

	
	struct inputVert
	{
		float4 vertex : POSITION;
		uint vid : SV_VertexID;
		float4 color : COLOR;
		
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2g
	{
		float4 vertex			: SV_POSITION;
		float4 color			: COLOR;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct g2f
	{
		float4 clipPos			: SV_POSITION;
		float4 color			: COLOR;
		float2 uv				: TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct Input
	{
		float vface : VFACE;
		fixed4 color : COLOR;
	};

	//Input Variables
	sampler2D _MainTex;
	float4 _MainTex_ST;
	half4 _Color;


	#if defined(ENABLE_INSTANCING)
	StructuredBuffer<PARTICLE_STRUCT> _ParticleBufferShader;
	#endif

	v2g vert(inputVert i)
	{
		v2g o;

		UNITY_INITIALIZE_OUTPUT(v2g, o);

		UNITY_SETUP_INSTANCE_ID(i);

		UNITY_TRANSFER_INSTANCE_ID(i, o);
		
		#if defined(ENABLE_INSTANCING)	
		PARTICLE_STRUCT p = _ParticleBufferShader[unity_InstanceID];
		//Manually Construct a matrix to transform the verts
		float4x4 matrix_ = (float4x4)0;
		matrix_._11_22_33_44 = float4(1.0, 1.0, 1.0, 1.0);
		matrix_._14_24_34 += p.position;
		
		o.vertex = mul(matrix_, i.vertex);

		//Standard initialization
		o.color = float4(p.color, p.alive);

		#else
		o.vertex = i.vertex;
		o.color = float4(1, 0, 1, 1);
		#endif
		return o;
	}

	void setup()
	{
	}

	
	[maxvertexcount(24)]
	void geom(point v2g IN[1], inout TriangleStream<g2f> triStream)
	{
		g2f o;
		
		UNITY_INITIALIZE_OUTPUT(g2f, o);
		UNITY_SETUP_INSTANCE_ID(IN[0]);
		UNITY_TRANSFER_INSTANCE_ID(IN[0], o);


		if(IN[0].color.a > 0)
		{
			float size = 1;
			
			float4 vert;

			float3 color = IN[0].color; 
			o.color = float4(color, 1);
			
			vert = IN[0].vertex + mul(float4(-.1, -.1, 0, 0) * size, UNITY_MATRIX_MV);
			o.clipPos = UnityObjectToClipPos(vert);
			o.uv = float2(0, 0);
			triStream.Append(o);

			vert = IN[0].vertex + mul(float4(-.1, .1, 0, 0) * size, UNITY_MATRIX_MV);
			o.clipPos = UnityObjectToClipPos(vert);
			o.uv = float2(0, 1);
			triStream.Append(o);

			vert = IN[0].vertex + mul(float4(.1, -.1, 0, 0) * size, UNITY_MATRIX_MV);
			o.clipPos = UnityObjectToClipPos(vert);
			o.uv = float2(1, 0);
			triStream.Append(o);

			vert = IN[0].vertex + mul(float4(.1, .1, 0, 0) * size, UNITY_MATRIX_MV);
			o.clipPos = UnityObjectToClipPos(vert);
			o.uv = float2(1, 1);
			triStream.Append(o);
		}
	}


	float4 frag(g2f i) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(i);

		float4 texSample = tex2D(_MainTex, _MainTex_ST.xy * i.uv + _MainTex_ST.zw);
		float4 col = i.color * _Color;
	
		return float4(col.rgb, col.a * texSample.a);
	}
	ENDCG
		
	SubShader
	{
		Pass
		{
			
			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
        	Cull Back

			CGPROGRAM
		
			#pragma vertex vert
			#pragma require geometry
			#pragma geometry geom
			#pragma fragment frag

			#pragma multi_compile_instancing

			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && defined(SUPPORT_STRUCTUREDBUFFER)
			#define ENABLE_INSTANCING
			#endif

			ENDCG
		}		
	}
}