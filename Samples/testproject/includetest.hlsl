// Randomly put this in a separate file for testing #include
float3 CalcDiffuseLighting(float3 N, float3 L, float3 Colour)
{
	return saturate( dot(N, L) ) * Colour;
}
