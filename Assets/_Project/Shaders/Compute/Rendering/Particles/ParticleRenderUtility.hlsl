#ifndef PARTICLE_RENDER_UTILITY
#define PARTICLE_RENDER_UTILITY

struct RenderVertex
{
    float3 positionWS;
    float3 normalWS;
    float4 tangentWS;
    float2 uv;
    float4 color;
};

struct RenderTri
{
    RenderVertex v[3];
    int subMeshId;
};

struct RenderQuad
{
    RenderVertex v[4];
};

AppendStructuredBuffer<RenderTri> _TriangleBuffer;


void Add_Render(RenderTri tri)
{
    _TriangleBuffer.Append(tri);
}

void Add_Render(RenderQuad quad)
{
    RenderTri tri = (RenderTri)0;
    tri.v[0] = quad.v[0];
    tri.v[1] = quad.v[1];
    tri.v[2] = quad.v[3];
    Add_Render(tri);
    
    tri.v[0] = quad.v[1];
    tri.v[1] = quad.v[2];
    tri.v[2] = quad.v[3];
    Add_Render(tri);
}

/// <summary>
/// Vertices must be oriented clockwise.
/// </summary>
void Add_Quad(RenderVertex v0, RenderVertex v1, RenderVertex v2, RenderVertex v3)
{
    RenderTri tri = (RenderTri)0;
    tri.v[0] = v0;
    tri.v[1] = v1;
    tri.v[2] = v3;
    Add_Render(tri);
    
    tri.v[0] = v1;
    tri.v[1] = v2;
    tri.v[2] = v3;
    Add_Render(tri);
}


// Quick render functions
void Add_BillboardRender(float3 position, float3 up, float3 right, float size = 1, float4 color = float4(1,1,1,1))
{
    float3 normal = cross(right, up);

    right *= 0.5 * size;
    up *= 0.5 * size;
    
    RenderTri tri = (RenderTri)0;
    tri.v[0].positionWS = position - up - right;
    tri.v[1].positionWS = position + up - right;
    tri.v[2].positionWS = position - up + right;

    tri.v[0].color = color;
    tri.v[1].color = color;
    tri.v[2].color = color;

    tri.v[0].normalWS = normal;
    tri.v[1].normalWS = normal;
    tri.v[2].normalWS = normal;

    tri.v[0].tangentWS = float4(right, 1);
    tri.v[1].tangentWS = float4(right, 1);
    tri.v[2].tangentWS = float4(right, 1);

    tri.v[0].uv = float2(0,0);
    tri.v[1].uv = float2(0,1);
    tri.v[2].uv = float2(1,0);

    Add_Render(tri);

    tri.v[0].positionWS = position + up - right;
    tri.v[1].positionWS = position + up + right;
    tri.v[2].positionWS = position - up + right;

    tri.v[0].uv = float2(0,1);
    tri.v[1].uv = float2(1,1);
    tri.v[2].uv = float2(1,0);

    Add_Render(tri);
}


RenderVertex ExtrudeVertexFromVertex(RenderVertex input, float3 direction, float radius)
{
    RenderVertex o;
    o.positionWS = input.positionWS + direction * radius;
    o.normalWS = direction;
    o.tangentWS = float4(input.normalWS, 1);
    o.color = input.color;
    o.uv = input.uv;
    return o;
}


RenderVertex ExtrudeVertexFromVertex(RenderVertex input, float3 direction, float radius, float uv_x)
{
    RenderVertex o;
    o.positionWS = input.positionWS + direction * radius;
    o.normalWS = direction;
    o.tangentWS = float4(input.normalWS, 1);
    o.color = input.color;
    o.uv = float2(input.uv.x + uv_x, input.uv.y);
    return o;
}


void Add_Tube4(RenderVertex startPoint, RenderVertex endPoint, float startRadius, float endRadius)
{
    RenderQuad quad = (RenderQuad)0;
    
    float3 startNormal      = startPoint.normalWS;
    float3 startTangent     = startPoint.tangentWS.xyz;
    float3 startBinormal    = cross(startNormal, startTangent);

    float3 endNormal        = endPoint.normalWS;
    float3 endTangent       = endPoint.tangentWS.xyz;
    float3 endBinormal      = cross(endNormal, endTangent);

    float3 startDirections[4] = { startTangent, startBinormal, -startTangent, -startBinormal };
    float3 endDirections[4] = { endTangent, endBinormal, -endTangent, -endBinormal };

    for(uint i = 0; i < 4; i++)
    {
        uint j = (i + 1) % 4;
        quad.v[0] = ExtrudeVertexFromVertex(startPoint, startDirections[i], startRadius, i / 4.0);
        quad.v[1] = ExtrudeVertexFromVertex(endPoint, endDirections[i], endRadius, i / 4.0);
        quad.v[2] = ExtrudeVertexFromVertex(endPoint, endDirections[j], endRadius, (i+1) / 4.0);
        quad.v[3] = ExtrudeVertexFromVertex(startPoint, startDirections[j], startRadius, (i+1) / 4.0);
        Add_Render(quad);
    }
}

void Add_Tube4Cap(RenderVertex startPoint, RenderVertex endPoint, float startRadius)
{
    RenderTri tri = (RenderTri)0;
    
    float3 startNormal      = startPoint.normalWS;
    float3 startTangent     = startPoint.tangentWS.xyz;
    float3 startBinormal    = cross(startNormal, startTangent);

    float3 startDirections[4] = { startTangent, startBinormal, -startTangent, -startBinormal };

    for (uint i = 0; i < 4; i++)
    {
        uint j = (i+1) % 4;
        tri.v[0] = ExtrudeVertexFromVertex(startPoint, startDirections[i], startRadius, i / 4.0);
        tri.v[1] = ExtrudeVertexFromVertex(endPoint, startNormal, 0, (i+0.5) / 4.0);
        tri.v[2] = ExtrudeVertexFromVertex(startPoint, startDirections[j], startRadius, (i + 1)/4.0);
        Add_Render(tri);
    }
}



void Add_Tube8(RenderVertex startPoint, RenderVertex endPoint, float startRadius, float endRadius)
{
    RenderQuad quad = (RenderQuad)0;
    
    float3 startNormal      = startPoint.normalWS;
    float3 startTangent     = startPoint.tangentWS.xyz;
    float3 startBinormal    = cross(startNormal, startTangent);
    
    
    float3 endNormal        = endPoint.normalWS;
    float3 endTangent       = endPoint.tangentWS.xyz;
    float3 endBinormal      = cross(endNormal, endTangent);

    float3 startDirections[8] = {
        startTangent, 0.707* (startTangent + startBinormal),
        startBinormal, 0.707* (startBinormal - startTangent),
        -startTangent, 0.707* (-startTangent - startBinormal),
        -startBinormal, 0.707* (- startBinormal + startTangent)
    };
    
    float3 endDirections[8] = {
        endTangent, 0.707 * (endTangent + endBinormal),
        endBinormal, 0.707 * (endBinormal - endTangent),
        -endTangent, 0.707 * (-endTangent - endBinormal),
        -endBinormal, 0.707 * (-endBinormal + endTangent )
    };

    for(uint i = 0; i < 8; i++)
    {
        uint j = (i + 1) % 8;
        quad.v[0] = ExtrudeVertexFromVertex(startPoint, startDirections[i], startRadius, i / 8.0);
        quad.v[1] = ExtrudeVertexFromVertex(endPoint, endDirections[i], endRadius, i / 8.0);
        quad.v[2] = ExtrudeVertexFromVertex(endPoint, endDirections[j], endRadius, (i+1) / 8.0);
        quad.v[3] = ExtrudeVertexFromVertex(startPoint, startDirections[j], startRadius, (i+1) / 8.0);
        Add_Render(quad);
    }
}

void Add_Tube8Cap(RenderVertex startPoint, RenderVertex endPoint, float startRadius)
{
    RenderTri tri = (RenderTri)0;
    
    float3 startNormal      = startPoint.normalWS;
    float3 startTangent     = startPoint.tangentWS.xyz;
    float3 startBinormal    = cross(startNormal, startTangent);

    float3 startDirections[8] = {
        startTangent, 0.707* (startTangent + startBinormal),
        startBinormal, 0.707* (startBinormal - startTangent),
        -startTangent, 0.707* (-startTangent - startBinormal),
        -startBinormal, 0.707* (- startBinormal + startTangent)
    };
    
    for (uint i = 0; i < 8; i++)
    {
        uint j = (i+1) % 8;
        tri.v[0] = ExtrudeVertexFromVertex(startPoint, startDirections[i], startRadius, i / 8.0);
        tri.v[1] = ExtrudeVertexFromVertex(endPoint, startNormal, 0, (i+0.5) / 8.0);
        tri.v[2] = ExtrudeVertexFromVertex(startPoint, startDirections[j], startRadius, (i + 1)/8.0);
        Add_Render(tri);
    }
}

#endif