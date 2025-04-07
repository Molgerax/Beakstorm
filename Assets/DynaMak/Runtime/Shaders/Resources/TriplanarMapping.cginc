#ifndef TRIPLANAR_MAPPING
#define TRIPLANAR_MAPPING


struct TriplanarUV
{
    float2 x;
    float2 y;
    float2 z;
};

TriplanarUV GetTriplanarUV(float3 p)
{
    TriplanarUV triUV;
    triUV.x = p.zy;
    triUV.y = p.xz;
    triUV.z = p.xy;
    return triUV;
}

TriplanarUV GetTriplanarUV(float3 p, float3 n)
{
    TriplanarUV triUV;
    triUV.x = p.zy;
    triUV.y = p.xz;
    triUV.z = p.xy;
    triUV.x.x *= sign(n.x);
    triUV.y.x *= sign(n.y);
    triUV.z.x *= sign(n.z);
    return triUV;
}

float3 GetTriplanarWeights(float3 normal)
{
    if(dot(normal, normal) == 0) return 0;
    float3 triW = abs(normal);
    return triW / (triW.x + triW.y + triW.z);
}

fixed4 SampleTextureTriplanar(sampler2D tex, float3 p, float3 n, float4 scaleOffset = float4(1,1,0,0))
{
    TriplanarUV triUV = GetTriplanarUV(p, n);
    float3 triWeights = GetTriplanarWeights(n);
    
    fixed4 xSample = tex2D(tex, triUV.x * scaleOffset.xy + scaleOffset.zw);
    fixed4 ySample = tex2D(tex, triUV.y * scaleOffset.xy + scaleOffset.zw);
    fixed4 zSample = tex2D(tex, triUV.z * scaleOffset.xy + scaleOffset.zw);

    return xSample * triWeights.x + ySample * triWeights.y + zSample * triWeights.z;
}

float3 BlendTriplanarNormal (float3 mappedNormal, float3 surfaceNormal) {
    float3 n;
    n.xy = mappedNormal.xy + surfaceNormal.xy;
    n.z = mappedNormal.z * surfaceNormal.z;
    return n;
}

float3 SampleNormalMapTriplanar(sampler2D tex, float3 p, float3 n, float4 scaleOffset = float4(1,1,0,0))
{
    TriplanarUV triUV = GetTriplanarUV(p, n);
    float3 triWeights = GetTriplanarWeights(n);
    
    float3 tangentNormalX = UnpackNormal(tex2D(tex, triUV.x * scaleOffset.xy + scaleOffset.zw));
    float3 tangentNormalY = UnpackNormal(tex2D(tex, triUV.y * scaleOffset.xy + scaleOffset.zw));
    float3 tangentNormalZ = UnpackNormal(tex2D(tex, triUV.z * scaleOffset.xy + scaleOffset.zw));

    float3 worldNormalX = BlendTriplanarNormal(tangentNormalX, n.zyx).zyx;
    float3 worldNormalY = BlendTriplanarNormal(tangentNormalY, n.xzy).xzy;
    float3 worldNormalZ = BlendTriplanarNormal(tangentNormalZ, n.xyz).xyz;

    //worldNormalX.z *= sign(n.x);
    //worldNormalY.z *= sign(n.y);
    //worldNormalZ.z *= sign(n.z);

    float3 mappedNormal = normalize(worldNormalX * triWeights.x + worldNormalY * triWeights.y + worldNormalZ * triWeights.z); 
    
    return mappedNormal;
}


#endif