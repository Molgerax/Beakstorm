#ifndef _INCLUDE_QUATERNION_UTILITY_
#define _INCLUDE_QUATERNION_UTILITY_

// Heavily pulled from https://gist.github.com/mattatz/40a91588d5fb38240403f198a938a593

#define QUATERNION_IDENTITY float4(0, 0, 0, 1)

#ifndef PI
#define PI 3.14159265359
#endif


inline float signedSaturate(float value)
{
    return sign(value) * saturate(abs(value));
}

inline float map01to11(float value)
{
    return saturate(value) * 2 - 1;
}

inline float map11to01(float value)
{
    return saturate(value * 0.5 + 0.5);
}

uint PackQuaternion(float4 q)
{
    return  (uint(floor(map11to01(q.w) * 255 + 0.5)) << 0) |
            (uint(floor(map11to01(q.z) * 255 + 0.5)) << 8) |
            (uint(floor(map11to01(q.y) * 255 + 0.5)) << 16) |
            (uint(floor(map11to01(q.x) * 255 + 0.5)) << 24);
}

float4 UnpackQuaternion(uint v)
{
    uint x = (v / (256 * 256 * 256)) % 256;
    uint y = (v / (256 * 256)) % 256;
    uint z = (v / 256) % 256;
    uint w = v % 256;

    return normalize(float4(
        map01to11(x / 255.0),
        map01to11(y / 255.0),
        map01to11(z / 255.0),
        map01to11(w / 255.0)));
}

uint2 PackQuaternion2(float4 q)
{
    return  uint2(
            (uint(floor(map11to01(q.w) * 65535 + 0.5)) << 0) |
            (uint(floor(map11to01(q.z) * 65535 + 0.5)) << 16),
            (uint(floor(map11to01(q.y) * 65535 + 0.5)) << 0) |
            (uint(floor(map11to01(q.x) * 65535 + 0.5)) << 16));
}

float4 UnpackQuaternion2(uint2 v)
{
    uint x = (v.y / 65536) % 65536;
    uint y = v.y % 65536;
    uint z = (v.x / 65536) % 65536;
    uint w = v.x % 65536;

    return float4(
        map01to11(x / 65535.0),
        map01to11(y / 65535.0),
        map01to11(z / 65535.0),
        map01to11(w / 65535.0));
}


inline float4 QuaternionMultiply(float4 qLhs, float4 qRhs)
{
    return float4(
        qRhs.xyz * qLhs.w + qLhs.xyz * qRhs.w + cross(qLhs.xyz, qRhs.xyz),
        qLhs.w * qRhs.w - dot(qLhs.xyz, qRhs.xyz)
    );
}

inline float4 QuaternionConjugate(float4 q)
{
    return q * float4(-1, -1, -1, 1);
}


inline float4 QuaternionInverse(float4 q)
{
    return QuaternionConjugate(q) / dot(q, q);
}

inline float3 RotateVectorByQuaternion(float3 v, float4 q)
{
    return QuaternionMultiply(q, QuaternionMultiply(float4(v, 0), QuaternionConjugate(q))).xyz;
}

float4 QuaternionAngleAxis(float angleRadians, float3 axis)
{
    float sine, cosine;
    sincos(angleRadians * 0.5, sine, cosine);
    return float4(axis * sine, cosine);
}

// https://stackoverflow.com/questions/1171849/finding-quaternion-representing-the-rotation-from-one-vector-to-another
float4 QuaternionFromTo(float3 v1, float3 v2)
{
    float4 q;
    float d = dot(v1, v2);
    if (d < -0.999999)
    {
        float3 right = float3(1, 0, 0);
        float3 up = float3(0, 1, 0);
        float3 tmp = cross(right, v1);
        if (length(tmp) < 0.000001)
        {
            tmp = cross(up, v1);
        }
        tmp = normalize(tmp);
        q = QuaternionAngleAxis(PI, tmp);
    } else if (d > 0.999999) {
        q = QUATERNION_IDENTITY;
    } else {
        q.xyz = cross(v1, v2);
        q.w = 1 + d;
        q = normalize(q);
    }
    return q;
}


float4 QuaternionLookAt(float3 forward, float3 up)
{
    float3 right = normalize(cross(forward, up));
    up = normalize(cross(forward, right));

    float m00 = right.x;
    float m01 = right.y;
    float m02 = right.z;
    float m10 = up.x;
    float m11 = up.y;
    float m12 = up.z;
    float m20 = forward.x;
    float m21 = forward.y;
    float m22 = forward.z;

    float num8 = (m00 + m11) + m22;
    float4 q = QUATERNION_IDENTITY;
    if (num8 > 0.0)
    {
        float num = sqrt(num8 + 1.0);
        q.w = num * 0.5;
        num = 0.5 / num;
        q.x = (m12 - m21) * num;
        q.y = (m20 - m02) * num;
        q.z = (m01 - m10) * num;
        return q;
    }

    if ((m00 >= m11) && (m00 >= m22))
    {
        float num7 = sqrt(((1.0 + m00) - m11) - m22);
        float num4 = 0.5 / num7;
        q.x = 0.5 * num7;
        q.y = (m01 + m10) * num4;
        q.z = (m02 + m20) * num4;
        q.w = (m12 - m21) * num4;
        return q;
    }

    if (m11 > m22)
    {
        float num6 = sqrt(((1.0 + m11) - m00) - m22);
        float num3 = 0.5 / num6;
        q.x = (m10 + m01) * num3;
        q.y = 0.5 * num6;
        q.z = (m21 + m12) * num3;
        q.w = (m20 - m02) * num3;
        return q;
    }

    float num5 = sqrt(((1.0 + m22) - m00) - m11);
    float num2 = 0.5 / num5;
    q.x = (m20 + m02) * num2;
    q.y = (m21 + m12) * num2;
    q.z = 0.5 * num5;
    q.w = (m01 - m10) * num2;
    return q;
}




float4x4 QuaternionToMatrix(float4 q)
{
    float4x4 m = 0;

    float x2 = q.x + q.x;
    float y2 = q.y + q.y;
    float z2 = q.z + q.z;
    float xx = q.x * x2;
    float xy = q.x * y2;
    float xz = q.x * z2;
    float yy = q.y * y2;
    float yz = q.y * z2;
    float zz = q.z * z2;
    float wx = q.w * x2;
    float wy = q.w * y2;
    float wz = q.w * z2;

    m[0][0] = 1.0 - (yy + zz);
    m[0][1] = xy - wz;
    m[0][2] = xz + wy;

    m[1][0] = xy + wz;
    m[1][1] = 1.0 - (xx + zz);
    m[1][2] = yz - wx;

    m[2][0] = xz - wy;
    m[2][1] = yz + wx;
    m[2][2] = 1.0 - (xx + yy);

    m[3][3] = 1.0;

    return m;
}


#endif