// Pixel shader for mipmap generation.
#include "GenMipsCommon.hlsl"

Texture2D tex;
SamplerState mySampler;

float4 Main(VSToPs In) : SV_Target
{
	float2 uv = In.UV;
	return tex.Sample(mySampler, uv);
}
