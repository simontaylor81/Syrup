// Simple shaders used by the framework.

cbuffer vbConstants
{
	float4x4	LocalToWorldMatrix;			// Transform from model space to world space.
	float4x4	WorldToProjectionMatrix;	// Transform from world space to projection space.
}

cbuffer psConstants
{
	float4		SolidColour = float4(1, 1, 1, 1);				// Solid colour to render in.
}

// Vertex shader input for a scene vertex.
struct VSSceneIn
{
	float3 Pos		: POSITION;
	float3 Normal	: NORMAL;
	float2 UVs[4]	: TEXCOORD0;
};

// Output of the basic scene vertex shader.
struct PSSceneIn
{
	float3 Normal	: TEXCOORD0;
	float4 UVs[2]	: TEXCOORD1;
	float3 WorldPos	: TEXCOORD3;
	float4 Pos		: SV_Position;
};

// Basic vertex shader for processing scene vertices.
PSSceneIn BasicSceneVS(VSSceneIn In)
{
	PSSceneIn Out;

	float4 WorldPos = mul(LocalToWorldMatrix, float4(In.Pos, 1.0f));
	Out.Pos = mul(WorldToProjectionMatrix, WorldPos);

	Out.WorldPos = WorldPos.xyz;
	
	// TODO: Transform normal to world space.
	Out.Normal = In.Normal;
	
	Out.UVs[0] = float4(In.UVs[0], In.UVs[1]);
	Out.UVs[1] = float4(In.UVs[2], In.UVs[3]);
		
	return Out;
}

// Pixel shader for very simple solid colour rendering.
float4 SolidColourPS(PSSceneIn In) : SV_Target
{
	return SolidColour;
}
