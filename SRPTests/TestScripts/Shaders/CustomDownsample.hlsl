
float4 Sample(float2 uv)
{
	return Texture.SampleLevel(LinearSampler, uv, DestMip - 1);
}
