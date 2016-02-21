// Custom 2D Texture downsample using linear sampler.
float4 Sample(float2 uv)
{
	return 1 - Texture.SampleLevel(LinearSampler, uv, DestMip - 1);
}
