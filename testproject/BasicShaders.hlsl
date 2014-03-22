// Simple shaders used by the framework.

cbuffer vbConstants
{
	float4x4	LocalToWorldMatrix = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};			// Transform from model space to world space.
	float4x4	WorldToProjectionMatrix;	// Transform from world space to projection space.
}

cbuffer psConstants
{
	float3		SolidColour = float3(1, 1, 0);				// Solid colour to render in.
	float		Alpha = 1.0f;
}

Texture2D DiffuseTex;

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
	float4 Pos : SV_Position;
};

// Vertex & pixel shaders for very simple solid colour rendering.
PSIn SolidColourVS(VSIn In)
{
	PSIn Out;

	Out.Pos = float4(In.Pos, 1.0f);
	Out.Pos = mul(LocalToWorldMatrix, Out.Pos);
	Out.Pos = mul(WorldToProjectionMatrix, Out.Pos);
	
	Out.Normal = In.Normal;
	for (int i = 0; i < 4; i++)
		Out.UVs[i] = In.UVs[i];
		
	return Out;
}

float4 SolidColourPS(PSIn In) : SV_Target
{
	float3 colour = SolidColour * (dot(In.Normal, float3(0,1,0)) * 0.5 + 0.5);
	return float4(colour, Alpha);
}

float4 TexturedPS(PSIn In) : SV_Target
{
	float3 colour = DiffuseTex.Sample(mySampler, In.UVs[0]);
	colour *= dot(In.Normal, float3(0,1,0)) * 0.5 + 0.5;
	return float4(colour, 1.0f);
}
