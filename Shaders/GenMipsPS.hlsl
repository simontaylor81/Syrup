// Pixel shader for mipmap generation.

#include "GenMipsCommon.hlsl"

// The texture we're downsampling, and a sampler to read it.
Texture2D Texture;
SamplerState Sampler;

// The mip that is being generated.
int DestMip;

// Include user-provided file containing the actual downsampling code.
#include "_scriptDownsample"

float4 Main(VSToPs In) : SV_Target
{
	// Call the function that should have been defined by the user.
	return Sample(In.UV);
}
