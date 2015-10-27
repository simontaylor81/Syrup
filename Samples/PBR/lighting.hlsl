// Various lighting and material stuff.

#include "material.hlsl"

struct BrdfParams
{
	float3 l;
	float3 v;
	float3 n;
	float3 h;
	float NoH;
	float NoL;
	float NoV;
	float LoH;
	MaterialParams MatParams;
};

float GGXAlpha(float smoothness)
{
	return Sqr(1 - smoothness);
}

float3 LambertDiffuse(MaterialParams matParams)
{
	// Simple Labertian diffuse -- constant colour.
	return matParams.DiffuseAlbedo;
	//return 0;
}

float3 BlinnSpecular(BrdfParams params)
{
	return pow(params.NoH, 15) / params.NoL;
}

float3 Fresnel(BrdfParams params)
{
	// Schlick approximation
	float3 f0 = params.MatParams.F0;
	return f0 + (1 - f0) * pow(1 - params.LoH, 5);
}

// Vis = G / (4 * n.l * n.v) 
float Visibility(BrdfParams params)
{
	// Schlick
	float k = GGXAlpha(params.MatParams.Smoothness) * 0.5;
	float g1l = params.NoL * (1 - k) + k;
	float g1v = params.NoV * (1 - k) + k;
	return 0.25 / (g1l * g1v);
}

float3 GGX(BrdfParams params)
{
	float alpha2 = Sqr(GGXAlpha(params.MatParams.Smoothness));
	return alpha2 / (PI * Sqr(Sqr(params.NoH) * (alpha2 - 1) + 1));
}

float3 NDF(BrdfParams params)
{
	return GGX(params);
}

float3 MicrofacetSpecular(BrdfParams params)
{
	return Fresnel(params) * Visibility(params) * NDF(params);
}

#define MICROFACET_SPECULAR 1
float3 Specular(BrdfParams params)
{
#if MICROFACET_SPECULAR
	return MicrofacetSpecular(params);
#else
	return BlinnSpecular(params);
#endif
}

float3 BRDF(BrdfParams params)
{
	return LambertDiffuse(params.MatParams) + Specular(params);
}

BrdfParams MakeParams(float3 n, float3 l, float3 v, MaterialParams matParams)
{
	BrdfParams params;
	
	params.l = l;
	params.v = v;
	params.n = n;
	params.h = normalize(l + v);
	params.NoH = saturate(dot(n, params.h));
	params.NoL = saturate(dot(n, l));
	params.NoV = max(dot(n, v), 1e-5);
	params.LoH = saturate(dot(l, params.h));
	params.MatParams = matParams;

	return params;
}

float3 DirectionalLight(float3 n, float3 l, float3 v, float3 colour, MaterialParams matParams)
{
	BrdfParams params = MakeParams(n, l, v, matParams);
	if (params.NoL > 0)
	{
		return BRDF(params) * colour * params.NoL;
	}
	return 0;
}

void MakeBasis(float3 n, out float3 tangentX, out float3 tangentY)
{
	float3 up = abs(n.z) < 0.999 ? float3(0,0,1) : float3(1,0,0);
	tangentX = normalize(cross(up, n) );
	tangentY = cross(n, tangentX);
}

// Taken from http://blog.selfshadow.com/publications/s2013-shading-course/karis/s2013_pbs_epic_notes_v2.pdf
float3 ImportanceSampleGGX(float2 xi, float3 n, float smoothness)
{
	float alpha2 = Sqr(GGXAlpha(smoothness));
	
	float phi = 2 * PI * xi.x;
	float cosTheta = sqrt((1 - xi.y) / (1 + (alpha2 - 1) * xi.y));
	float sinTheta = sqrt(1 - cosTheta * cosTheta);
	
	float3 h;
	h.x = sinTheta * cos(phi);
	h.y = sinTheta * sin(phi);
	h.z = cosTheta;
	
	float3 tangentX, tangentY;
	MakeBasis(n, tangentX, tangentY);
	
	// Tangent to world space
	return tangentX * h.x + tangentY * h.y + n * h.z;
}

// Taken from http://blog.selfshadow.com/publications/s2013-shading-course/karis/s2013_pbs_epic_notes_v2.pdf
float3 SpecularIBL(float3 n, float3 v, TextureCube cube, MaterialParams matParams, uint2 random)
{
	float3 result = 0;

	const uint numSamples = 1024;
	for (uint i = 0; i < numSamples; i++ )
	{
		float2 xi = Hammersley(i, numSamples, random);
		float3 h = ImportanceSampleGGX(xi, n, matParams.Smoothness);
		float3 l = 2 * dot(v, h) * h - v;
		
		BrdfParams params;
		params.l = l;
		params.v = v;
		params.n = n;
		params.h = h;
		params.NoH = saturate(dot(n, h));
		params.NoL = saturate(dot(n, l));
		params.NoV = max(dot(n, v), 1e-5);
		params.LoH = saturate(dot(l, h));
		float VoH = saturate(dot(v, h));
		
		params.MatParams = matParams;

		if (params.NoL > 0)
		{
			//return n;
		
			float3 sampleColour = cube.SampleLevel(mySampler, l, 0).rgb;
			float vis = Visibility(params);
			float Fc = pow(1 - VoH, 5);
			float3 F = (1 - Fc) * matParams.F0 + Fc;
			
			// Incident light = sampleColour * NoL
			// Microfacet specular = D * G * F / (4*NoL * NoV)
			// pdf = D * NoH / (4 * VoH)
			result += sampleColour * F * vis * 4 * params.NoL * VoH / params.NoH;
		}
	}
	
	//return n;
	return result / numSamples;
}

float3 IBL(float3 n, float3 v, TextureCube cube, MaterialParams matParams, uint2 random)
{
	// TODO: Diffuse IBL
	return SpecularIBL(n, v, cube, matParams, random);
}
