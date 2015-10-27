// Physically based shizzle.

#include "util.hlsl"

// Some defaults
#ifndef PBR_USE_IBL
#define PBR_USE_IBL 1
#endif

cbuffer vbConstants
{
	float4x4	LocalToWorldMatrix = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};			// Transform from model space to world space.
	float4x4	WorldToProjectionMatrix;	// Transform from world space to projection space.
}

cbuffer psConstants
{
	float3	DirLightVector = float3(1, 1, 0);	// Light vector for directional light.
	float3 	DirLightColour = float3(1, 1, 1);
	float	Ambient = 0.1;
	
	float3 CameraPosition;
}

Texture2D DiffuseTex;
TextureCube EnvCube;

SamplerState mySampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSIn
{
	float3 Pos : POSITION;
	float3 Normal : NORMAL;
	float2 UVs[4] : TEXCOORD0;
};
struct PSIn
{
	float3 Normal : TEXCOORD0;
	float2 UVs[4] : TEXCOORD1;
	float3 WorldPos : TEXCOORD5;
	float4 Pos : SV_Position;
};

#include "lighting.hlsl"

// Very basic vertex shader.
PSIn BasicVS(VSIn In)
{
	PSIn Out;

	float4 WorldPos = mul(LocalToWorldMatrix, float4(In.Pos, 1.0f));
	Out.Pos = mul(WorldToProjectionMatrix, WorldPos);
	Out.WorldPos = WorldPos.xyz;
	
	Out.Normal = mul(LocalToWorldMatrix, float4(In.Normal, 0.0f)).xyz;
	for (int i = 0; i < 4; i++)
		Out.UVs[i] = In.UVs[i];
		
	return Out;
}

// Pixel shader for very simple solid colour rendering.
float4 SolidColourPS(PSIn In) : SV_Target
{
	float3 N = normalize(In.Normal);
	float3 V = normalize(CameraPosition - In.WorldPos);
	float3 R = 2 * dot(N, V) * N - V;
	
	// Pseudo-random number for jittering, etc.
	uint2 random = ScrambleTEA(asuint(In.Pos.xy));
	
	MaterialParams matParams = GetMaterialParams(In.UVs[0]);

	// Ambient lighting
	float3 lighting = Ambient * (dot(In.Normal, float3(0,1,0)) * 0.5 + 0.5);
	
	lighting += DirectionalLight(N, normalize(DirLightVector), V, DirLightColour, matParams);
	
#if PBR_USE_IBL
	lighting += IBL(N, V, EnvCube, matParams, random);
#endif
	
	return float4(lighting, 1.0f);
}

// Pixel shader for simple textured rendering
float4 TexturedPS(PSIn In) : SV_Target
{
	float3 colour = DiffuseTex.Sample(mySampler, In.UVs[0]).rgb;
	colour *= dot(In.Normal, float3(0,1,0)) * 0.5 + 0.5;
	return float4(colour, 1.0f);
}
