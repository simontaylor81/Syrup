// Physically based shizzle.

cbuffer vbConstants
{
	float4x4	LocalToWorldMatrix = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};			// Transform from model space to world space.
	float4x4	WorldToProjectionMatrix;	// Transform from world space to projection space.
}

cbuffer psConstants
{
	float3	SolidColour = float3(1, 1, 0);		// Solid colour to render in.
	float3	LightVector = float3(0, 1, 0);		// Light vector for directional light.
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

// Very basic vertex shader.
PSIn BasicVS(VSIn In)
{
	PSIn Out;

	float4 WorldPos = mul(LocalToWorldMatrix, float4(In.Pos, 1.0f));
	Out.Pos = mul(WorldToProjectionMatrix, WorldPos);
	Out.WorldPos = WorldPos.xyz;
	
	Out.Normal = In.Normal;
	for (int i = 0; i < 4; i++)
		Out.UVs[i] = In.UVs[i];
		
	return Out;
}

// Pixel shader for very simple solid colour rendering.
float4 SolidColourPS(PSIn In) : SV_Target
{
	float3 N = In.Normal;
	float3 V = normalize(CameraPosition - In.WorldPos);
	float3 R = 2 * dot(N, V) * N - V;

	// Ambient lighting
	float lighting = Ambient * (dot(In.Normal, float3(0,1,0)) * 0.5 + 0.5);
	
	// Directional light (lambert).
	lighting += dot(In.Normal, normalize(LightVector));
	
	float3 env = EnvCube.Sample(mySampler, R).rgb;
	
	float3 colour = SolidColour * lighting + env;
	//return float4(colour, 1.0f);
	
	return float4(env, 1.0f);
}

// Pixel shader for simple textured rendering
float4 TexturedPS(PSIn In) : SV_Target
{
	float3 colour = DiffuseTex.Sample(mySampler, In.UVs[0]).rgb;
	colour *= dot(In.Normal, float3(0,1,0)) * 0.5 + 0.5;
	return float4(colour, 1.0f);
}
