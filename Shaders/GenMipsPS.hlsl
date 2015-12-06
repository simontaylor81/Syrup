// Pixel shader for mipmap generation.
#include "GenMipsCommon.hlsl"

float4 Main(VSToPs In) : SV_Target
{
	float2 uv = In.UV;
	return float4(uv.x, uv.y, 0, 1);
}
