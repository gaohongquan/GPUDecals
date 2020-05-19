
#ifndef __CULLING_INCLUDE
#define __CULLING_INCLUDE

//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "UnityCG.cginc"

CBUFFER_START(_cluster_)
    uint g_ClusterNumX;
    uint g_ClusterNumY;
    uint g_ClusterNumZ;
    float g_zCullingRange;
    float g_ClusterRate;
CBUFFER_END

CBUFFER_START(_tilebased_)
       uint g_TilebasedSize;
       uint g_TileNumX;
       uint g_TileNumY;
CBUFFER_END

CBUFFER_START(_index_)
        uint g_PreClusterMaxNumElements;
        uint g_PreClusterMaxNumIndex;
        uint g_CullTestSpheresNum;
CBUFFER_END

float ComputeSquaredDistanceToAABB(float3 Pos, float3 AABBCenter, float3 AABBHalfSize)
{
    float3 delta = max(0, abs(AABBCenter - Pos) - AABBHalfSize);
    return dot(delta, delta);
}

bool TestSphereVsAABB(float3 sphereCenter, float sphereRadius, float3 AABBCenter, float3 AABBHalfSize)
{
    float distSq = ComputeSquaredDistanceToAABB(sphereCenter, AABBCenter, AABBHalfSize);
    return distSq <= sphereRadius * sphereRadius;
}

uint GetClusterIndex(float2 ScreenUV, float linearDepth)
{
    int zindex = pow(linearDepth / g_zCullingRange, 1.0 / g_ClusterRate) * g_ClusterNumZ;
    int xindex = floor(ScreenUV.x * g_ClusterNumX);
    int yindex = floor(ScreenUV.y * g_ClusterNumY);
    return xindex + yindex * g_ClusterNumX + zindex * g_ClusterNumX * g_ClusterNumY;
}

uint GetTileIndex(float2 ScreenPos)
{
    float fTileRes = (float)g_TilebasedSize;
    uint nTileIdx = floor(ScreenPos.x / fTileRes) + floor(ScreenPos.y / fTileRes) * g_TileNumX;
    return nTileIdx;
}

inline float3 ComputeViewSpacePosition(float2 positionSS, float linearDepth, float4x4 invProjMatrix)
{
    float2 positionNDC = positionSS * 2 - 1;
    float3 positionCS = float3(positionNDC.x, positionNDC.y, 1.0) * _ProjectionParams.z;
    return mul(invProjMatrix, positionCS.xyzz).xyz * linearDepth;
}

#endif //__CULLING_INCLUDE
