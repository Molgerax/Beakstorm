#ifndef _INCLUDE_SDF_UTILITY_
#define _INCLUDE_SDF_UTILITY_

// ------- SDF STRUCTS

#define SDF_SPHERE 0
#define SDF_BOX 1
#define SDF_LINE 2
#define SDF_CONE 3
#define SDF_TORUS 4

#define PHEROMONE_ONLY 6

struct AbstractSdfData
{
    float3 XAxis;
    float3 YAxis;
    float3 ZAxis;
    float3 Translate;
    float3 Data;
    uint Type;
};

struct SdfQueryInfo
{
    float dist;
    float3 normal;
    uint matIndex;
};

// ------- FORWARD DECLARATION

SdfQueryInfo sdfGeneric(float3 p, AbstractSdfData data);

SdfQueryInfo sdfSphere(float3 p, AbstractSdfData data);
SdfQueryInfo sdfBox(float3 p, AbstractSdfData data);
SdfQueryInfo sdfLine(float3 p, AbstractSdfData data);
SdfQueryInfo sdfCone(float3 p, AbstractSdfData data);
SdfQueryInfo sdfTorus(float3 p, AbstractSdfData data);


float3 getLargest(float3 value);
inline float3 inverseLerp(float3 from, float3 to, float3 value);
float3 ClosestPointOnBox(float3 center, float3 bounds, float3 p);

// ------- SDF FUNCTIONS

SdfQueryInfo sdfGeneric(float3 p, AbstractSdfData data)
{
    SdfQueryInfo result;
    result.dist = 1.#INF;
    result.normal = float3(0,1,0);
    result.matIndex = (data.Type >> 4) & 0x0F;
    
    const uint type = data.Type & 0x0F;
    
    if (type == SDF_SPHERE)
        return sdfSphere(p, data);
    
    if (type == SDF_BOX)
        return sdfBox(p, data);
    
    if (type == SDF_LINE)
        return sdfLine(p, data);
    
    if (type == SDF_CONE)
        return sdfCone(p, data);
    
    if (type == SDF_TORUS)
        return sdfTorus(p, data);

    return result;
}


/// <summary>
/// Gets the signed distance of point p to the surface of a sphere.
/// </summary>
/// <param name="center">Center of the sphere</param>
/// <param name="radius">Radius of the sphere</param>
/// <param name="p">Input position</param>
float sdfSphere(float3 center, float radius, float3 p)
{
    return distance(center, p) - radius;
}


SdfQueryInfo sdfSphere(float3 p, AbstractSdfData data)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.dist = -data.Data.x;
    result.normal.y = 1;
    result.matIndex = (data.Type >> 4) & 0x0F;

    
    float3 diff = p - data.Translate;
    if (dot(diff, diff) == 0)
        return result;
    
    float len = length(diff);
    result.dist = len - data.Data.x;
    
    result.normal = diff / len;
    return result;
}

/// <summary>
/// Gets the signed distance of point p to the surface of a box.
/// </summary>
/// <param name="center">Center of the box</param>
/// <param name="bounds">Bounds of the box</param>
/// <param name="p">Input position</param>
float sdfBox(float3 center, float3 bounds, float3 p)
{
    float3 q = abs(p - center) - bounds;
    return length(max(q, 0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float3 sdfBoxNormal(float3 center, float3 bounds, float3 p)
{
    float3 q = (p - center);
    float3 diff = abs(q) - bounds;
    float3 norm = (getLargest(diff) + max(diff, 0)) * sign(q);
    if (dot(norm, norm) == 0)
        norm = float3(0,0,0);
    return (norm);
}


/// <summary>
/// Gets the signed distance of point p to the surface of a box.
/// </summary>
/// <param name="center">Center of the box</param>
/// <param name="bounds">Bounds of the box</param>
/// <param name="xAxis">x-Axis of the box (must be normalized)</param>
/// <param name="yAxis">y-Axis of the box (must be normalized)</param>
/// <param name="p">Input position</param>
float sdfBox(float3 center, float3 bounds, float3 xAxis, float3 yAxis, float3 p)
{
    float3 zAxis = cross(xAxis, yAxis);
    float3 q = p - center;
    float3 diff = abs( float3( dot(q, xAxis), dot(q, yAxis), dot(q, zAxis) )) - bounds;
    
    return length(max(diff, 0)) + min(max(diff.x, max(diff.y, diff.z)), 0.0);
}


SdfQueryInfo sdfBox(float3 p, AbstractSdfData data, float l)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.normal.y = 1;
    result.matIndex = (data.Type >> 4) & 0x0F;
    
    float3 q = p - data.Translate;
    float3 diff = abs( float3( dot(q, data.XAxis), dot(q, data.YAxis), dot(q, data.ZAxis) )) - data.Data;

    result.dist = length(max(diff, 0)) + min(max(diff.x, max(diff.y, diff.z)), 0);
    result.normal = diff;
    
    return result;
}


SdfQueryInfo sdfBox(float3 p, AbstractSdfData data)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.normal.y = 1;
    result.matIndex = (data.Type >> 4) & 0x0F;

    float3x3 rot = float3x3(data.XAxis, data.YAxis, data.ZAxis);
    
    float3 q = mul(rot, p - data.Translate);
    float3 diff = abs(q) - data.Data;
    
    result.dist = length(max(diff, 0)) + min(max(diff.x, max(diff.y, diff.z)), 0);
    
    float3 norm = (getLargest(diff) + max(diff, 0)) * sign(q);
    if (dot(norm, norm) == 0)
        norm = float3(0,1,0);
    result.normal = mul(transpose(rot), normalize(norm));
    
    return result;
}


/// <summary>
/// Gets the signed distance of point p to the line between two points.
/// </summary>
/// <param name="pointA">Position of Point A</param>
/// <param name="pointB">Position of Point B</param>
/// <param name="radius">Thickness of the line</param>
/// <param name="p">Input position</param>
float sdfLine(float3 pointA, float3 pointB, float radius, float3 p)
{
    float3 pa = p - pointA;
    float3 ba = pointB - pointA;
    float h = clamp( dot(pa, ba) / dot(ba, ba), 0.0, 1.0 );
    return length( pa - ba * h) - radius;
}

/// <summary>
/// Gets the vector tp point p from the closest point on the line between two points.
/// </summary>
/// <param name="pointA">Position of Point A</param>
/// <param name="pointB">Position of Point B</param>
/// <param name="p">Input position</param>
float3 sdfLine_Vector(float3 pointA, float3 pointB, float3 p)
{
    float3 pa = p - pointA;
    float3 ba = pointB - pointA;
    float h = clamp( dot(pa, ba) / dot(ba, ba), 0.0, 1.0 );
    return pa - ba * h;
}


SdfQueryInfo sdfLine(float3 p, AbstractSdfData data)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.dist = -data.Data.x;
    result.normal.y = 1;
    result.matIndex = (data.Type >> 4) & 0x0F;

    float3x3 rot = float3x3(data.XAxis, data.YAxis, data.ZAxis);
    float3 q = mul(rot, p - data.Translate);
    q.z -= clamp (q.z, -data.Data.y * 0.5, data.Data.y * 0.5);

    if (dot(q, q) == 0)
        return result;

    q = mul(transpose(rot), q);
    float len = length(q);

    result.normal = q / len;
    result.dist = (len - data.Data.x);
    
    return result;
}



/// x: RadiusBase, y: RadiusTip, z: Height
SdfQueryInfo sdfCone(float3 p, AbstractSdfData data)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.dist = -data.Data.y;
    result.normal.y = 0;
    result.matIndex = (data.Type >> 4) & 0x0F;

    float3 xAxis = cross(data.YAxis, data.ZAxis);
    float3x3 rot = float3x3(xAxis, data.YAxis, data.ZAxis);
    float3 q = mul(rot, p - data.Translate);
    float3 qb = mul(rot, p - data.Translate - data.ZAxis * data.Data.z);
    
    float b = (data.Data.x - data.Data.y) / data.Data.z;
    float a = sqrt(1.0 - b*b);
    float2 t = float2( length(q.xy), q.z);
    float k = dot(t, float2(-b, a));
    
    float3 ql = float3(q.x / t.x, q.y / t.x, b);
    
    if (k < 0)
    {
        result.dist = length(t) - data.Data.x;
        result.normal = normalize(mul(transpose(rot), q));
    }
    else if (k > a * data.Data.z)
    {
        result.dist = length(t - float2(0, data.Data.z)) - data.Data.y;
        result.normal = normalize(mul(transpose(rot), qb));
    }
    else
    {
        result.dist = dot(t, float2(a, b)) - data.Data.x;
        result.normal = normalize(mul(transpose(rot), ql));
    }
    return result;
}




/// <summary>
/// Gets the signed distance of point p to a torus.
/// </summary>
/// <param name="center">Center of the torus</param>
/// <param name="normal">Normal of the torus (must be normalized)</param>
/// <param name="radius">Radius of the torus from its center</param>
/// <param name="thickness">Thickness of the torus ring</param>
/// <param name="p">Input position</param>
float sdfTorus(float3 center, float3 normal, float radius, float thickness, float3 p)
{
    float3 q = p - center;
    float h = dot(q, normal);

    float3 tangentPlane = q - normal * h;
    if(dot(tangentPlane, tangentPlane) == 0) return sqrt(h * h + radius * radius) - thickness;

    float3 tCenter = normalize(tangentPlane) * radius;
    return length(q - tCenter) - thickness;
}


/// <summary>
/// Gets the vector from point p to a torus. (not normalized)
/// </summary>
/// <param name="center">Center of the torus</param>
/// <param name="normal">Normal of the torus (must be normalized)</param>
/// <param name="radius">Radius of the torus from its center</param>
/// <param name="thickness">Thickness of the torus ring</param>
/// <param name="p">Input position</param>
float3 sdfTorus_Vector(float3 center, float3 normal, float radius, float thickness, float3 p)
{
    float3 q = p - center;
    float h = dot(q, normal);

    // Tangent of the Plane
    float3 tangentPlane = q - normal * h;
    if(dot(tangentPlane, tangentPlane) == 0) tangentPlane = cross(normal, normal + 0.1);
    
    float3 torusVector = q - normalize(tangentPlane) * radius;
    
    return normalize(torusVector) * (length(torusVector) - thickness);
}

/// <summary>
/// Gets the tangent at the closest point on a torus from point p.
/// Ideal for adding curl in a fluid field.
/// </summary>
/// <param name="center">Center of the torus</param>
/// <param name="normal">Normal of the torus (must be normalized)</param>
/// <param name="radius">Radius of the torus from its center</param>
/// <param name="thickness">Thickness of the torus ring</param>
/// <param name="p">Input position</param>
float3 sdfTorus_Tangent(float3 center, float3 normal, float radius, float thickness, float3 p)
{
    float3 q = p - center;
    float h = dot(q, normal);

    // Tangent of the Plane
    float3 tangentPlane = q - normal * h;
    if(dot(tangentPlane, tangentPlane) == 0) return normal;
    
    tangentPlane = normalize(tangentPlane);
    float3 binormal = cross(normal, tangentPlane);
    float3 torusVector = q - tangentPlane * radius;
    
    return normalize(cross(binormal, torusVector));
}


SdfQueryInfo sdfTorus(float3 p, AbstractSdfData data)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.dist = -data.Data.x;
    result.normal.y = 1;
    result.matIndex = (data.Type >> 4) & 0x0F;
    
    result.dist = sdfTorus(data.Translate, data.YAxis, data.Data.x, data.Data.y, p);
    result.normal = normalize(sdfTorus_Vector(data.Translate, data.YAxis, data.Data.x, data.Data.y, p));
    
    return result;
}



// ------- SDF OPERATORS

float opUnion(float d1, float d2)
{
    return min(d1,d2);
}

float opSubtraction(float d1, float d2)
{
    return max(-d1,d2);
}

float opIntersection(float d1, float d2)
{
    return max(d1,d2);
}

float opSmoothUnion( float d1, float d2, float k )
{
    float h = clamp( 0.5 + 0.5*(d2-d1)/k, 0.0, 1.0 );
    return lerp( d2, d1, h ) - k*h*(1.0-h);
}

float opSmoothSubtraction( float d1, float d2, float k )
{
    float h = clamp( 0.5 - 0.5*(d2+d1)/k, 0.0, 1.0 );
    return lerp( d2, -d1, h ) + k*h*(1.0-h);
}

float opSmoothIntersection( float d1, float d2, float k )
{
    float h = clamp( 0.5 - 0.5*(d2-d1)/k, 0.0, 1.0 );
    return lerp( d2, d1, h ) + k*h*(1.0-h);
}



// ------- CLOSEST POINT FUNCTIONS

// INVERSE LERP

inline float inverseLerp(float from, float to, float value)
{
    return (value -from) / (to - from);
}
inline float2 inverseLerp(float2 from, float2 to, float2 value)
{
    return (value -from) / (to - from);
}
inline float3 inverseLerp(float3 from, float3 to, float3 value)
{
    return (value -from) / (to - from);
}


float3 getLargest(float3 value)
{
    float3 firstTest = step(value.yzx, value);
    float3 secondTest = step(value.zxy, value);
    return firstTest * secondTest;
}

float3 InsideBox(float3 center, float3 bounds, float3 p)
{
    float3 uv = ((p - center) / bounds);
    return step(0, 0.5 - abs(uv));
}

float3 ClosestPointOnBox(float3 center, float3 bounds, float3 p)
{
    float3 boxmin = center - bounds;
    float3 boxmax = center + bounds;

    
    float3 relative = saturate(inverseLerp(boxmin, boxmax, p));
    float3 snapped = step(0.5, relative);
    float3 closest = getLargest(abs(relative - 0.5));
    
    
    return lerp(boxmin, boxmax, snapped) * closest + (1 - closest) * p;
}


// ------- MISC HELPER FUNCTIONS

/// <summary>
/// Snaps a normalized vector to the 6 cardinal directions.
/// </summary>
float3 Vector_Snap_to_Grid(float3 v)
{
    if(dot(v,v) == 0) return float3(0, 1, 0);
    
    float3 s = sign(v);
    float3 vAbs = abs(v);
    float biggest = max(vAbs.x, max(vAbs.y, vAbs.z));
    return step(1, vAbs / biggest) * s;
}

#endif