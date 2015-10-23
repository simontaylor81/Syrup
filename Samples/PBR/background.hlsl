// Shaders for rendering the environment cube map into the background.

cbuffer vbConstants
{
	float4x4	ProjectionToWorldMatrix;
}

cbuffer psConstants
{
}

TextureCube EnvCube;

SamplerState mySampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct PSIn
{
	float4 Pos	: SV_Position;
	float2 UV	: TEXCOORD0;
	float3 ViewVec : TEXCOORD1;
};

PSIn VS(float4 Pos : POSITION)
{
	PSIn Result;
	Result.Pos = Pos;

	Result.ViewVec = mul(ProjectionToWorldMatrix, float4(Pos.xy, 1, 1));
	
	return Result;
}

float4 PS(PSIn In) : SV_Target
{
	float3 env = EnvCube.Sample(mySampler, In.ViewVec).rgb;
	return float4(env, 1);
}
