// Simple shaders used by the framework.

cbuffer vbConstants
{
	float4x4	LocalToWorldMatrix = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};			// Transform from model space to world space.
	float4x4	WorldToProjectionMatrix;	// Transform from world space to projection space.
}

cbuffer psPerScene
{
	float3 CameraPos;
	float Ambient = 0.1f;
}

cbuffer psLight
{
	float3	LightPos = float3(0, 2, 0);
	float3	LightColour = float3(1, 1, 1);
	float 	LightInvSqrRadius;
}

Texture2D DiffuseTex;
Texture2D NormalTex;

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
	float4 UVs[2] : TEXCOORD1;
	float3 WorldPos : TEXCOORD3;
	float4 Pos : SV_Position;
};

// Simple vertex shader.
PSIn VSMain(VSIn In)
{
	PSIn Out;

	float4 WorldPos = mul(LocalToWorldMatrix, float4(In.Pos, 1.0f));
	Out.Pos = mul(WorldToProjectionMatrix, WorldPos);

	Out.WorldPos = WorldPos.xyz;
	
	Out.Normal = In.Normal;
	
	Out.UVs[0] = float4(In.UVs[0], In.UVs[1]);
	Out.UVs[1] = float4(In.UVs[2], In.UVs[3]);
		
	return Out;
}

//--------------------------------------------------------------------------------------
// Evaluate and accumulate lighting for a light given its params.
//--------------------------------------------------------------------------------------
void AccumulateLight(float3 LightPos, float3 Colour, float InvSqrRadius,
					 float3 SurfacePos, float3 N, float3 V,
					 inout float3 DiffuseLighting, inout float3 SpecLighting)
 {
	// Point light direction and attenuation.
	float3 LightVec = LightPos - SurfacePos;
	float LightDistance = dot(LightVec, LightVec) * InvSqrRadius;
	float LightAtten = pow(saturate(1 - LightDistance), 2);
	float3 L = normalize(LightVec);
	
	// Diffuse lighting.
	DiffuseLighting += saturate( dot(N, L) ) * Colour * LightAtten;
	
	// Accumulate specular lighting.
	float3 H = normalize(V + L);
	SpecLighting += pow( saturate(dot(H, N)), 32 ) * Colour * LightAtten;
}


//--------------------------------------------------------------------------------------
// Forward lighting pixel shader.
//--------------------------------------------------------------------------------------
float4 ForwardLightPS(PSIn In) : SV_Target
{
	float3 N = normalize(In.Normal);
	float3 V = normalize(CameraPos - In.WorldPos);

	// Initialise diffuse with half-lambert ambient.
	float3 DiffuseLighting = (dot(N, float3(0,1,0)) * 0.5 + 0.5) * Ambient;
	float3 SpecLighting = float3(0,0,0);
	
	AccumulateLight(LightPos, LightColour, LightInvSqrRadius,
		In.WorldPos, N, V,
		DiffuseLighting, SpecLighting);
	
	float4 DiffuseSample = DiffuseTex.Sample(mySampler, In.UVs[0].xy);
	float3 colour = DiffuseSample.xyz * DiffuseLighting
		+ DiffuseSample.w * SpecLighting;
	
	return float4(colour, 1.0f);
}

