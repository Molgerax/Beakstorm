// Pull in URP library functions and our own common functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "../QuaternionUtility.hlsl"


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
	float4 color : COLOR; // Color
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
	float3 positionWS	: TEXCOORD2;
	float2 screenUV		: TEXCOORD3;
	DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
	uint instanceID		: SV_InstanceID;
};

float3 hsv2rgb(float3 hsv)
{
	float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);

	return hsv.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);
}

Interpolators Vertex(Attributes input, uint instance_id: SV_InstanceID)
{
	Interpolators output;

	Boid boid = _BoidBuffer[instance_id];
    
	float3 meshPositionWS = boid.pos;
	float4 rotation = boid.rotation;//UnpackQuaternion(boid.rotation);

	float3x3 rotMatrix = QuaternionToMatrix(rotation);
	//rotMatrix = transpose(rotMatrix);

	float wingFlap = boid.data * PI * 2;
	float wing = input.color.r;
	float3 pos = input.positionOS;

	pos.y += wing * sin(wingFlap) * 0.5 * (1 - boid.exposure * 0.5);
	pos.z -= wing * 0.32 * (boid.exposure);
	
	float3 worldPos = mul(rotMatrix, pos * _Size) + meshPositionWS;
	
	output.positionCS = TransformWorldToHClip(worldPos);

	float4 color = input.color;
	color.a = saturate(boid.exposure);

	color.xyz = lerp(color.xyz, RotateVectorByQuaternion(float3(0,0,1), rotation) * 0.5 + 0.5, _VertexColorToBase);
	
	output.color = color;
	output.normalWS = mul(rotMatrix, input.normalOS);
	output.uv = input.uv;
	output.positionWS = worldPos;
	output.screenUV = GetNormalizedScreenSpaceUV(output.positionCS);

	output.instanceID = instance_id;
	
	OUTPUT_LIGHTMAP_UV( vert.uv, unity_LightmapST, output.lightmap);
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

	return output;
}

Interpolators DepthOnlyVertex(Attributes input, uint instance_id: SV_InstanceID)
{
	Interpolators output = (Interpolators)0;

	Boid boid = _BoidBuffer[instance_id];
    
	float3 meshPositionWS = boid.pos;
	float4 rotation = boid.rotation;//UnpackQuaternion(boid.rotation);

	float3x3 rotMatrix = QuaternionToMatrix(rotation);
	//rotMatrix = transpose(rotMatrix);
	
	float3 worldPos = mul(rotMatrix, input.positionOS * _Size) + meshPositionWS;

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


// The fragment function. This runs once per fragment, which you can think of as a pixel on the screen
// It must output the final color of this pixel
float4 Fragment(Interpolators input) : SV_TARGET{
	
	float2 uv = input.uv;
	// Sample the color map
	float4 colorSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
	half3 l = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

	float3 cameraDiff = GetCameraPositionWS() - input.positionWS;
	float distanceToCam = length(cameraDiff);
	distanceToCam = saturate(distanceToCam / 10);
	float2 screenUv = GetNormalizedScreenSpaceUV(input.positionCS.xy);
	float dither = Dither(distanceToCam, screenUv);
	clip(dither);

	float distanceToCamBorder = length(cameraDiff);
	distanceToCamBorder = 1 - saturate((distanceToCamBorder-960) / 64);
	float3 q = abs(input.positionWS - _SimulationCenter) - _SimulationSize * 0.5;

	float distanceToCamBorderWalls = min(0, max(q.x, max(q.y, q.z)));
	distanceToCamBorderWalls = saturate(-distanceToCamBorderWalls / 64);
	distanceToCamBorder = max(distanceToCamBorder, distanceToCamBorderWalls);
	
	float distanceDither = Dither(distanceToCamBorder, screenUv);
	clip(distanceDither);

	float wing = input.color.r;
	float beak = input.color.g;
	float exposure = input.color.a;

	beak = lerp(1, beak, _VertexColorToEmissive);
	
	float smoothness = lerp(0.5, 1, exposure) * beak;
	float metallic = lerp(0, 1, exposure) * beak;

	metallic = lerp(metallic, 0, _VertexColorToBase);
	smoothness = lerp(smoothness, 0, _VertexColorToBase);

	float3 color = colorSample.rgb * _Color.rgb * exposure * beak;
	color = lerp(color, input.color.xyz, _VertexColorToBase);
	
	// For lighting, create the InputData struct, which contains position and orientation data
	InputData lightingInput = (InputData)0; // Found in URP/ShaderLib/Input.hlsl
	lightingInput.positionWS = input.positionWS;
	lightingInput.normalWS = normalize(input.normalWS);
	lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); // In ShaderVariablesFunctions.hlsl
	lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS); // In Shadows.hlsl
	lightingInput.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, input.normalWS);
	lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = color;
	surfaceInput.alpha = 1;
	surfaceInput.specular = 1;
	surfaceInput.smoothness = smoothness;
	surfaceInput.metallic = metallic;
	surfaceInput.occlusion = 1;
	
	#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
	float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
	//surfaceInput.occlusion *= aoFactor.directAmbientOcclusion;
	#endif

	
	return UniversalFragmentPBR(lightingInput, surfaceInput);
}