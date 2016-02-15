// Pixel shader for mipmap generation.

#include "GenMipsCommon.hlsl"

// The texture we're downsampling.
#if SRP_MIPGEN_2D
Texture2D Texture;
#else
TextureCube Texture;
#endif

// Samplers that the user code can use.
SamplerState LinearSampler;
SamplerState PointSampler;

// The mip that is being generated.
uint DestMip;

// Index of the array slice being generated.
uint ArraySlice;

// Include user-provided file containing the actual downsampling code.
#include "_scriptDownsample"

#if SRP_MIPGEN_2D

float4 Main(VSToPs In) : SV_Target
{
	// Call the function that should have been defined by the user.
	return Sample(In.UV);
}

#else // Cubemap

float3 GetCubeCoords(float2 uv, uint face)
{
	float u = 2.0f * uv.x - 1.0f;
	float v = 2.0f * uv.y - 1.0f;

	switch (face)
	{
	case 0: return float3(1.0f, -v, -u);
	case 1: return float3(-1.0f, -v, u);
	case 2: return float3(u, 1.0f, v);
	case 3: return float3(u, -1.0f, -v);
	case 4: return float3(u, -v, 1.0f);
	case 5: return float3(-u, -v, -1.0f);
	}

	// Should never happen.
	return 0;
}

float4 Main(VSToPs In) : SV_Target
{
	// Convert to cube map coordinates.
	uint face = ArraySlice % 6;
	float3 uvw = GetCubeCoords(In.UV, face);

	// Call the function that should have been defined by the user.
	return Sample(uvw);
}

#endif
