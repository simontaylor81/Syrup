// Pixel shader for mipmap generation.

#include "GenMipsCommon.hlsl"

// The texture we're downsampling.
Texture2D Texture;

// Samplers that the user code can use.
SamplerState LinearSampler;
SamplerState PointSampler;

// The mip that is being generated.
int DestMip;

// Include user-provided file containing the actual downsampling code.
#include "_scriptDownsample"

float4 Main(VSToPs In) : SV_Target
{
	// Call the function that should have been defined by the user.
	return Sample(In.UV);
}
