Shader "Beakstorm/TriplanarClouds_Tesselated"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _Noise("Base Map", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 1
        _ScrollingSpeed("Scrolling Speed", Float) = 1
        _Height("Height", Float) = 1
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Float) = 1
        
        
        _Normal("Normal", Range(0.0, 1.0)) = 1
        
        [Header(Tesselation)]
        _TesselationAmount("Tesselation Amount", Range(1.0, 64.0)) = 1.0
        _TesselationFadeStart("Tesselation Fade Start", Float) = 1.0
        _TesselationFadeEnd("Tesselation Fade End", Float) = 1.0
    }

    HLSLINCLUDE


    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "../Graphs/Subgraphs/GetMainLight.hlsl"
    #include "../Compute/SimplexNoise.hlsl"
    
    struct Attributes
    {
        float4 color : COLOR;
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float2 uv : TEXCOORD0;
        float4 tangent : TANGENT;
    };

    struct TesselationControlPoint
    {
        float3 positionWS : INTERNALTESSPOS;
        float2 uv : TEXCOORD0;
        float3 normalWS : TEXCOORD1;
        float3 tangentWS : TEXCOORD2;
    };

    struct TesselationFactors
    {
        float edge[3] : SV_TessFactor;
        float inside : SV_InsideTessFactor;
    };

    struct Varyings
    {
        float4 positionHCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
        float3 normalWS : TEXCOORD2;
        float3 tangentWS : TEXCOORD3;
    };

    TEXTURE2D(_Noise);
    SAMPLER(sampler_Noise);

    CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        float4 _Noise_ST;
        float _NoiseScale;
        float _ScrollingSpeed;
        float _Height;
        half4 _RimColor;
        float _RimPower;
        float _Normal;
        float _TesselationAmount;
        float _TesselationFadeStart;
        float _TesselationFadeEnd;
    CBUFFER_END

    
    TesselationControlPoint vert(Attributes IN)
    {
        TesselationControlPoint o;

        float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
        float3 worldNormal = TransformObjectToWorldNormal(IN.normalOS.xyz);
        
        o.positionWS = worldPos;
        o.uv = TRANSFORM_TEX(IN.uv, _Noise);
        o.normalWS = worldNormal;
        o.tangentWS = TransformObjectToWorldDir(IN.tangent.xyz);// * (IN.tangent.w);
        return  o;
    }

    [domain("tri")]
    [outputcontrolpoints(3)]
    [outputtopology("triangle_cw")]
    [partitioning("fractional_even")]
    [patchconstantfunc("PatchConstantFunc")]
    TesselationControlPoint hull(InputPatch<TesselationControlPoint, 3> patch, uint id : SV_OutputControlPointID)
    {
        return patch[id];
    }

    TesselationFactors PatchConstantFunc(InputPatch<TesselationControlPoint, 3> patch)
    {
        TesselationFactors f = (TesselationFactors)0;

        float3 triPos0 = patch[0].positionWS;
        float3 triPos1 = patch[1].positionWS;
        float3 triPos2 = patch[2].positionWS;

        float3 edgePos0 = 0.5f * (triPos1 + triPos2);
        float3 edgePos1 = 0.5f * (triPos2 + triPos0);
        float3 edgePos2 = 0.5f * (triPos0 + triPos1);

        float3 camPos = GetCameraPositionWS();

        float dist0 = distance(edgePos0, camPos);
        float dist1 = distance(edgePos1, camPos);
        float dist2 = distance(edgePos2, camPos);

        float fadeDist = _TesselationFadeEnd - _TesselationFadeStart;

        float edgeFactor0 = saturate(1.0 - (dist0 - _TesselationFadeStart) / fadeDist);
        float edgeFactor1 = saturate(1.0 - (dist1 - _TesselationFadeStart) / fadeDist);
        float edgeFactor2 = saturate(1.0 - (dist2 - _TesselationFadeStart) / fadeDist);

        f.edge[0] = max(edgeFactor0 * _TesselationAmount, 1);
        f.edge[1] = max(edgeFactor1 * _TesselationAmount, 1);
        f.edge[2] = max(edgeFactor2 * _TesselationAmount, 1);

        f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0;
        
        return f;
    }

    float displacementFactor(float3 worldPos, float3 worldNormal)
    {
        float timeOffset = _Time.x * _ScrollingSpeed;

        //timeOffset = frac(timeOffset) * PI * 2;
        
        float3 samplePos = worldPos / _NoiseScale;
        float3 noiseXY = SAMPLE_TEXTURE2D_LOD(_Noise, sampler_Noise, samplePos.xy + timeOffset, 0);
        float3 noiseYZ = SAMPLE_TEXTURE2D_LOD(_Noise, sampler_Noise, samplePos.yz + timeOffset, 0);
        float3 noiseZX = SAMPLE_TEXTURE2D_LOD(_Noise, sampler_Noise, samplePos.zx + timeOffset, 0);
        float3 noiseCombine = noiseXY;
        noiseCombine = lerp(noiseCombine, noiseZX, worldNormal.y);
        noiseCombine = lerp(noiseCombine, noiseYZ, worldNormal.x);
        
        float noise = noiseCombine.x;

        //noise = SimplexNoise4D(float4(samplePos, timeOffset));
        noise = SimplexNoise3D((samplePos + timeOffset));

        float lacunarity = 1;
        float scale = 1;
        float weightSum = 1;
        for (int i = 0; i < 2; i++)
        {
            lacunarity *= 0.5;
            scale *= 2;
            weightSum += lacunarity;
            noise += SimplexNoise3D((samplePos + timeOffset) * scale) * lacunarity;
        }

        noise /= weightSum;
        
        noise = saturate(noise * 0.5 + 0.5);
        return (noise);
    }
    
    float3 displacement(float3 worldPos, float3 worldNormal)
    {
        float noise = displacementFactor(worldPos, worldNormal);

        return worldPos + worldNormal * noise * _Height;
    }
    

    [domain("tri")]
    Varyings domain(TesselationFactors factors, OutputPatch<TesselationControlPoint, 3> patch,
        float3 barycentricCoordinates : SV_DomainLocation)
    {
        Varyings o = (Varyings)0;

        float3 positionWS =
            patch[0].positionWS * barycentricCoordinates.x +
            patch[1].positionWS * barycentricCoordinates.y +
            patch[2].positionWS * barycentricCoordinates.z;

        float2 uv = 
            patch[0].uv * barycentricCoordinates.x +
            patch[1].uv * barycentricCoordinates.y +
            patch[2].uv * barycentricCoordinates.z;

        float3 worldNormal =
            patch[0].normalWS * barycentricCoordinates.x +
            patch[1].normalWS * barycentricCoordinates.y +
            patch[2].normalWS * barycentricCoordinates.z;
        worldNormal = SafeNormalize(worldNormal);        

        float3 worldTangent =
            patch[0].tangentWS * barycentricCoordinates.x +
            patch[1].tangentWS * barycentricCoordinates.y +
            patch[2].tangentWS * barycentricCoordinates.z;
        worldTangent = normalize(worldTangent);
        
        float3 worldBinormal = SafeNormalize(cross(worldNormal, worldTangent));

        float offset = 0.1;

        float3 displaced = displacement(positionWS, worldNormal);
        float3 displacedBi = displacement(positionWS + worldBinormal * offset, worldNormal);
        float3 displacedTan = displacement(positionWS + worldTangent * offset, worldNormal);


        float d = _Height * displacementFactor(positionWS, worldNormal);
        float dBinormal = _Height * displacementFactor(positionWS + worldBinormal * offset, worldNormal) - d;
        float dTangent = _Height * displacementFactor(positionWS + worldTangent * offset, worldNormal) - d;

        float3 newNormal = cross(displacedTan - displaced, displacedBi - displaced);

        dBinormal /= offset;
        dTangent /= offset;
        
        newNormal = worldNormal - (dBinormal) * worldBinormal - (dTangent) * worldTangent;

        if (dot(newNormal, newNormal) > 0)
            newNormal = normalize(newNormal);
        else
            newNormal = worldNormal;
        

        worldNormal = normalize(lerp(worldNormal, newNormal, _Normal));
        
        positionWS = displaced;
        
        o.positionWS = positionWS;
        o.positionHCS = TransformWorldToHClip(positionWS);
        o.uv = uv;
        o.normalWS = worldNormal;
        o.tangentWS = worldTangent;

        return o;
    }

    half4 frag(Varyings IN) : SV_Target
    {
        float3 color = _BaseColor.rgb;

        float3 worldPos = IN.positionWS;
        float3 worldNormal = (IN.normalWS);
        
        float noise = displacementFactor(worldPos, worldNormal);

        float3 viewDir = SafeNormalize(GetWorldSpaceViewDir(worldPos));

        float rim = 1 - saturate(dot(viewDir, noise * worldNormal));
        float3 rimColor = _RimColor.rgb * pow(rim, _RimPower);

        LightingLambertShaded_float(worldPos, 0, worldNormal, viewDir, 0, color);
        color *= _BaseColor;
        color.rgb += rimColor * _RimColor.a;
        
        return half4(color, 1);
    }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain

            
            ENDHLSL
        }
    }
}
