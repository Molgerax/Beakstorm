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
	float3 normalWS		: TEXCOORD1;
	float4 tangent		: TEXCOORD2;
	float3 positionWS	: TEXCOORD3;
	float2 screenUV		: TEXCOORD4;
	DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
	uint instanceID		: SV_InstanceID;
};

Interpolators Vertex(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
{
	Interpolators output;
	RenderTri tri = _TriangleBufferShader[instance_id];
	RenderVertex vert = tri.v[2 - vertex_id];
	float3 worldPos = vert.positionWS;

	output.positionCS = TransformWorldToHClip(worldPos);

	output.color = vert.color;
	output.normalWS = vert.normalWS;
	output.tangent = vert.tangentWS;
	output.uv = vert.uv;
	output.positionWS = vert.positionWS;
	output.screenUV = GetNormalizedScreenSpaceUV(output.positionCS);

	output.instanceID = instance_id;
	
	OUTPUT_LIGHTMAP_UV( vert.uv, unity_LightmapST, output.lightmap);
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
	
	
	
	if(tri.subMeshId != _SubMeshId)
		output.positionCS.w = 0.0 / 0.0;

	return output;
}

Interpolators DepthOnlyVertex(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
{
	Interpolators output;
	RenderTri tri = _TriangleBufferShader[instance_id];
	RenderVertex vert = tri.v[2 - vertex_id];
	float3 worldPos = vert.positionWS;

	output.positionCS = TransformWorldToHClip(worldPos);
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
	// Sample the color map
	float4 colorSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
	half3 l = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

	// For lighting, create the InputData struct, which contains position and orientation data
	InputData lightingInput = (InputData)0; // Found in URP/ShaderLib/Input.hlsl
	lightingInput.positionWS = input.positionWS;
	lightingInput.normalWS = normalize(input.normalWS);
	lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); // In ShaderVariablesFunctions.hlsl
	lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS); // In Shadows.hlsl
	lightingInput.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, input.normalWS);
	lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = colorSample.rgb * _Color.rgb;
	surfaceInput.alpha = colorSample.a * _Color.a;
	surfaceInput.specular = 1;
	surfaceInput.smoothness = 0.5;
	surfaceInput.occlusion = 1;
	
	#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
	float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
	//surfaceInput.occlusion *= aoFactor.directAmbientOcclusion;
	#endif

	
	return UniversalFragmentPBR(lightingInput, surfaceInput);
}