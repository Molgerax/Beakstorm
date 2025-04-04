#ifndef SDF_UTILITY
#define SDF_UTILITY

// ------ SDF STRUCTS -------

struct AABB
{
    float3 center;
    float3 bounds;
};

struct Box
{
    float3 center;
    float3 bounds;
    float3 xAxis;   //must be Normalized
    float3 yAxis;   //must be Normalized
};

struct Sphere
{
    float3 center;
    float radius;
};

struct Line
{
    float3 pointA;
    float3 pointB;
    float radius;
};

struct Torus
{
    float3 center;
    float3 normal;
    float radius;
    float thickness;
};

// ------- SDF FUNCTIONS


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