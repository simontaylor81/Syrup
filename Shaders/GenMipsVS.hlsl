// Vertex shader for mipmap generation.
#include "GenMipsCommon.hlsl"

// Simple fullscreen quad shader.
VSToPs Main(float4 Pos : POSITION)
{
	VSToPs Result;
	Result.Pos = Pos;

	Result.UV = Pos.xy * 0.5 + 0.5;
	Result.UV.y = 1 - Result.UV.y;

	return Result;
}
