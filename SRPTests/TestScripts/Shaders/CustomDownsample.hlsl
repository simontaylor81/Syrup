
float4 Sample(float2 uv)
{
	return Texture.SampleLevel(Sampler, uv, DestMip - 1);
}
