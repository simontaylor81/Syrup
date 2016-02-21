// Custom 2D texture downsample using point sampler.
float4 Sample(float2 uv)
{
	return 1 - Texture.SampleLevel(PointSampler, uv, DestMip - 1);
}
