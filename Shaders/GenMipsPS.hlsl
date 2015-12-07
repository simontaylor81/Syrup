// Pixel shader for mipmap generation.
#include "GenMipsCommon.hlsl"

Texture2D tex;
SamplerState mySampler;

// The mip that is being generated.
int DestMip;

float4 Main(VSToPs In) : SV_Target
{
	float2 uv = In.UV;
	return tex.SampleLevel(mySampler, uv, DestMip - 1);
}
