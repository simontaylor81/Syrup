// Code shader by mipmap generation pixel and vertex shaders.
// Note that they're not just in the same file as the PS needs to #include the custom sampling code.

// Data passed from VS to PS.
struct VSToPs
{
	float2 UV	: TEXCOORD0;
	float4 Pos	: SV_Position;
};
