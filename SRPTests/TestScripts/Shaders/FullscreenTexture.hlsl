// Shaders for drawing a texture to the render target.

Texture2D tex;

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
