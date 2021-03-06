﻿#ifndef _YUN_DECALS_H
#define _YUN_DECALS_H

struct DecalData
{
	float4x4 worldToLocal;
	float4 uv;
	float2 tiling;
	float alpha;
	float normalIntensity;
};

struct DecalInput
{
	float4 PositionCS;
	float3 PositionWS;
	float3 NormalWS;
};

sampler2D g_DecalBaseMap;
sampler2D g_DecalNormalMap;

#define _MOBILE_MAX_DECALDATA_SIZE 128

#if defined(_CULLING_CLUSTER_ON)
#include "Culling.cginc"
uint g_DecalClusterMaxNumElements;
StructuredBuffer<uint> g_AdditiveDecalIndexBuffer;
#endif

uint g_AdditiveDecalCount;

#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
CBUFFER_START(_DecalDatasBuffer)
float4x4 g_DecalWorldToLocals[_MOBILE_MAX_DECALDATA_SIZE];
float4 g_Decaluvs[_MOBILE_MAX_DECALDATA_SIZE];
float2 g_Mixs[_MOBILE_MAX_DECALDATA_SIZE];
CBUFFER_END
#else
StructuredBuffer<DecalData> g_AdditiveDecalDatasBuffer;
#endif

DecalData GetDecalData(uint index)
{
#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	DecalData decal;
	decal.worldToLocal = g_DecalWorldToLocals[index];
	decal.uv = g_Decaluvs[index];
	decaluv.tiling = g_Mixs[index].xy;
	decal.alpha = g_Mixs[index].z;
	decal.normalIntensity = g_Mixs[index].w;
	return decal;
#else
	return g_AdditiveDecalDatasBuffer[index];
#endif
}

int CheckAdditionalDecal(float4x4 worldToDecalMatrix, DecalInput input,out float2 decaluv)
{
	float3 decalPos = mul(worldToDecalMatrix, float4(input.PositionWS, 1.0));
	float3 local = abs(decalPos);
	int check = step(local.x, 0.5) + step(local.y, 0.5) + step(local.z, 0.5);

	float3 decalNormal = mul(worldToDecalMatrix, float4(input.NormalWS, 0.0)).xyz;
	decalPos.xz -= decalPos.y * decalNormal.xz;
	local = abs(decalPos);
	check += (step(local.x, 0.5) + step(local.y, 0.5) + step(local.z, 0.5));

	decaluv.xy = decalPos.xz;
	return check / 6;
}

void AdditionDecal(DecalData decal, float2 decaluv, inout half3 albedo, inout float3 normalTS)
{
	float2 texcoord = frac((decaluv.xy + 0.5) * decal.tiling);
	float2 uv = lerp(decal.uv.xy, decal.uv.zw, texcoord.xy);

	half4 col = tex2D(g_DecalBaseMap, uv);
	half3 nl = UnpackNormal(tex2D(g_DecalNormalMap, uv));
	nl.xy *= decal.normalIntensity;
	albedo = lerp(albedo, col.rgb, col.a * decal.alpha);
	normalTS = lerp(normalTS, nl, col.a * sign(decal.normalIntensity));
}

void AdditionalDetalData(DecalData decal, DecalInput input, inout half3 albedo, inout float3 normalTS)
{
	float2 decaluv;
	if (CheckAdditionalDecal(decal.worldToLocal, input, decaluv) < 1)
		return;
	AdditionDecal(decal, decaluv, albedo, normalTS);
}

void AdditionalDetal(DecalInput input,inout half3 albedo,inout float3 normalTS)
{
#if defined(_CULLING_CLUSTER_ON)
	float3 screenPos = input.PositionCS.xyz / input.PositionCS.w;
	float depth;
#if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
	depth = Linear01Depth((screenPos.z + 1.0f) * 0.5f);
#else
	depth = Linear01Depth(screenPos.z);
#endif

	int depthCheck = step(depth / g_zCullingRange, 1);
	int cIndex = GetClusterIndex(screenPos.xy, depth) * depthCheck;
	int nStartIndex = cIndex * g_DecalClusterMaxNumElements;
	int nDecalCount = g_AdditiveDecalIndexBuffer[nStartIndex] * depthCheck;
	nStartIndex += 1;
	nDecalCount = nStartIndex + nDecalCount;
	[loop]
	for (uint i = nStartIndex; i < nDecalCount; i++)
	{
		DecalData decal = GetDecalData(g_AdditiveDecalIndexBuffer[i]);
		AdditionalDetalData(decal, input, albedo, normalTS);
	}
#else
	[loop]
	for (int i = 0; i < g_AdditiveDecalCount; i++)
	{
		DecalData decal = GetDecalData(i);
		AdditionalDetalData(decal, input, albedo, normalTS);
	}
#endif
}

#endif //_YUN_DECALS_H
