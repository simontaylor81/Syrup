// Simple shaders used by the framework.

cbuffer vsConstants
{
	float4x4	LocalToWorldMatrix = {1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1};			// Transform from model space to world space.
	float4x4	WorldToProjectionMatrix;	// Transform from world space to projection space.
}

cbuffer cbBasePassPS
{
	float Ambient = 0.1f;
}

cbuffer cbDeferredPassPS
{
	float4x4	ProjectionToWorldMatrix;	// Inverse view-projection matrix.
	float3 CameraPosition;
}

cbuffer vsPerLight
{
	float3	vsLightPos = float3(0, 2, 0);
	float 	vsLightRadius;
}

cbuffer psPerLight
{
	float3	LightPos = float3(0, 2, 0);
	float3	LightColour = float3(1, 1, 1);
	float 	LightInvSqrRadius;
}

// Material textures.
Texture2D DiffuseTex;
Texture2D NormalTex;

// GBuffer textures.
Texture2D GBuffer_Albedo;
Texture2D GBuffer_Normal;
Texture2D GBuffer_Depth;

SamplerState LinearSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};
SamplerState GBufferSampler
{
	Filter = MIN_MAG_MIP_POINT;
	AddressU = Clamp;
	AddressV = Clamp;
};

struct VSSceneIn
{
	float3 Pos : POSITION;
	float3 Normal : NORMAL;
	float2 UVs[4] : TEXCOORD0;
};
struct PSSceneIn
{
	float3 Normal : TEXCOORD0;
	float4 UVs[2] : TEXCOORD1;
	float3 WorldPos : TEXCOORD3;
	float4 Pos : SV_Position;
};
struct PSDeferredIn
{
	float4 Pos : SV_Position;		// vertex position
	float4 ScreenUV : TEXCOORD0;	// Screen-space texture coordinates.
};

//------------------------------------------------------------------------------
// Shaders for the initial gbuffer pass.
//------------------------------------------------------------------------------

// GBuffer fill vertex shader.
PSSceneIn BasePass_VS(VSSceneIn In)
{
	PSSceneIn Out;

	float4 WorldPos = mul(LocalToWorldMatrix, float4(In.Pos, 1.0f));
	Out.Pos = mul(WorldToProjectionMatrix, WorldPos);

	Out.WorldPos = WorldPos.xyz;
	
	Out.Normal = In.Normal;
	
	Out.UVs[0] = float4(In.UVs[0], In.UVs[1]);
	Out.UVs[1] = float4(In.UVs[2], In.UVs[3]);
		
	return Out;
}

// Gbuffer fill pixel shader.
void BasePass_PS(PSSceneIn In,	
				 out float4 OutLighting : SV_Target0,
				 out float4 OutNormal : SV_Target1,
				 out float4 OutAlbedo : SV_Target2
				 )
{
	float3 N = normalize(In.Normal);

	// Write albedo + specular to albedo buffer.
	OutAlbedo = DiffuseTex.Sample(LinearSampler, In.UVs[0].xy);

	// Initialise lighting buffer with half-lambert ambient.
	float3 ambient = (dot(N, float3(0,1,0)) * 0.5 + 0.5) * Ambient * OutAlbedo.xyz;
	OutLighting = float4(ambient, 1.0f);
	
	// Normal buffer just has the normal in it.
	// Scale/bias into [0,1] range to pack into 8-bit RT.
	OutNormal = float4(N * 0.5f + 0.5f, 0);
}


//------------------------------------------------------------------------------
// Shaders for the deferred passes.
//------------------------------------------------------------------------------

// Deferred pass vertex shader.
PSDeferredIn DeferredPass_VS(VSSceneIn In)
{
	PSDeferredIn Out;

	float4 WorldPos = float4(In.Pos * vsLightRadius + vsLightPos, 1.0f);
	Out.Pos = mul(WorldToProjectionMatrix, WorldPos);

	Out.ScreenUV = Out.Pos;

	return Out;    
}


// Deferred shading deferred pass that uses the g-buffer to accumulate lighting.

float4 DeferredPass_PS(PSDeferredIn In) : SV_Target
{
	float2 ScreenPos = In.ScreenUV.xy / In.ScreenUV.w;
	
//	return float4(1,0,0,1);

	// Read the depth from the depth buffer.
	// TODO: Convert to Load().
	float2 UV = ScreenPos * 0.5f + 0.5f;
	UV.y = 1 - UV.y;
	float Depth = GBuffer_Depth.Sample(GBufferSampler, UV).r;
	
//	return (1.0f - Depth) * 10;
//	return float4(ScreenPos, 0, 1);

	// Convert position back into world space.
	float4 WorldPosHom = mul(ProjectionToWorldMatrix, float4(ScreenPos, Depth, 1));
	float3 WorldPos = WorldPosHom.xyz / WorldPosHom.w;

//	return float4(WorldPos.xyz, 1.0f);
	//return WorldPosHom.w/1000;

	// Point light direction and attenuation.

	float3 LightVec = LightPos - WorldPos;
	float LightDistance = dot(LightVec, LightVec) * LightInvSqrRadius;
	float LightAtten = pow(saturate(1 - LightDistance), 2);
	float3 NormalisedLightVec = normalize(LightVec);

	// Discard irrelevant pixels.
	if (LightAtten < 0.0001)
		discard;

	// Sample normal and albedo buffers.
	float3 Normal = GBuffer_Normal.Sample(GBufferSampler, UV).rgb * 2 - 1;
	float4 Albedo = GBuffer_Albedo.Sample(GBufferSampler, UV);

	// Diffuse lighting.
	float3 DiffuseLighting = saturate( dot(Normal, NormalisedLightVec) ) * Albedo.rgb;

	// Specular lighting.
	float3 ViewVec = normalize(CameraPosition - WorldPos);
	float3 HalfAngle = normalize(ViewVec + NormalisedLightVec);
	float3 SpecLighting = pow( saturate(dot(HalfAngle, Normal)), 32 ) * Albedo.a;

	float3 FinalColour = (DiffuseLighting + SpecLighting) * LightAtten * LightColour;
	return float4(FinalColour, 1);
}

