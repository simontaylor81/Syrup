// Shaders for drawing a texture to the render target.

Texture2D tex;
TextureCube texCube;

float MipLevel;

SamplerState mySampler;

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

// Some completely arbitrary projection to fit the whole
// cubemap on-screen somehow.
float3 CubemapProjection(float2 screenPos)
{
	float pi = 3.14159;

	float theta = screenPos.x * pi;
	float phi = screenPos.y * pi / 2;
	
	float x = cos(theta) * cos(phi);
	float y = sin(phi);
	float z = sin(theta) * cos(phi);

	return float3(x, y, z);
}

float4 FullscreenTextureCube_PS(PSIn In) : SV_Target
{
	float3 v = CubemapProjection(2 * In.UV - 1);
	return texCube.Sample(mySampler, v);
}

float4 FullscreenTextureCubeLevel_PS(PSIn In) : SV_Target
{
	float3 v = CubemapProjection(2 * In.UV - 1);
	return texCube.SampleLevel(mySampler, v, MipLevel);
}