// Custom cubemap Texture downsample.
float4 Sample(float3 uvw)
{
	float4 value = Texture.SampleLevel(LinearSampler, uvw, DestMip - 1);
	
	// Invert to be sure we're using the shader path. Don't
	// invert the alpha channel though, as it makes the results invisible.
	return float4(1 - value.rgb, value.a);
}
