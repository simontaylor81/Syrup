// Shaders for drawing a texture to the render target.

Texture2D tex;
TextureCube texCube;

float MipLevel;

SamplerState mySampler
{
    Filter = MIN_MAG_MIP_NEAREST;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct PSIn
{
	float4 Pos	: SV_Position;
	float2 UV	: TEXCOORD0;
};

PSIn FullscreenTexture_VS(float4 Pos : POSITION)
{
	PSIn Result;
	Result.Pos = Pos;

	Result.UV = Pos.xy * 0.5 + 0.5;
	Result.UV.y = 1 - Result.UV.y;
	
	return Result;
}

float4 FullscreenTexture_PS(PSIn In) : SV_Target
{
	return tex.Sample(mySampler, In.UV);
}

float4 FullscreenTextureLevel_PS(PSIn In) : SV_Target
{
	return tex.SampleLevel(mySampler, In.UV, MipLevel);
}

float4 FullscreenTextureCube_PS(PSIn In) : SV_Target
{
	float pi = 3.14159;

	// Uses some completely arbitrary projection to fit the whole
	// cubemap on-screen somehow.
	float2 screenPos = (2 * In.UV - 1);
	float theta = screenPos.x * pi;
	float phi = screenPos.y * pi / 2;
	
	float x = cos(theta) * cos(phi);
	float y = sin(phi);
	float z = sin(theta) * cos(phi);
	
	return texCube.Sample(mySampler, float3(x, y, z));
}
