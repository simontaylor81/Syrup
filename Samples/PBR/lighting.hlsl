// Various lighting and material stuff.

float3 LambertDiffuse()
{
	// Simple Labertian diffuse -- constant colour.
	return BaseColour;
}

float3 BlinnSpecular(float3 l, float3 v, float3 n, float3 h)
{
	return pow(saturate(dot(n, h)), 15) / (dot(n, l));
}

float3 MicrofacetSpecular(float3 l, float3 v, float3 n, float3 h)
{
	return 0;
}

#define MICROFACET_SPECULAR 1
float3 Specular(float3 l, float3 v, float3 n, float3 h)
{
#ifdef MICROFACET_SPECULAR
	return MicrofacetSpecular(l, v, n, h);
#else
	return BlinnSpecular(l, v, n, h) + 1;
#endif
}

float3 BRDF(float3 l, float3 v, float3 n)
{
	float3 h = normalize(l + v);
	return LambertDiffuse() + BlinnSpecular(l, v, n, h);
}

float3 DirectionalLight(float3 n, float3 l, float3 v, float3 colour)
{
	return BRDF(l, v, n) * colour * (dot(n, l));
	return BRDF(l, v, n);
}
