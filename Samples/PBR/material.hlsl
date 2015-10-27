
float3 BaseColour = float3(1, 1, 1);
Texture2D BaseColourTex;
Texture2D SmoothnessTex;
Texture2D MetallicTex;

// (Smoothness, Metallic, Reflectance)
float3 PbrParams = float3(1, 1, 0.5);

struct MaterialParams
{
	float3 DiffuseAlbedo;
	float Smoothness;
	float3 F0;
};

MaterialParams GetMaterialParams(float2 uv)
{
	MaterialParams result;

	float3 baseColour = BaseColour * BaseColourTex.Sample(mySampler, uv).rgb;
	float smoothness = PbrParams.x * SmoothnessTex.Sample(mySampler, uv).r;
	float metallic = PbrParams.y * MetallicTex.Sample(mySampler, uv).r;
	float reflectance = PbrParams.z;
	
	result.DiffuseAlbedo = lerp(baseColour, 0, metallic);
	result.Smoothness = smoothness;
	result.F0 = lerp(reflectance * 0.08, baseColour, metallic);
	
	return result;
}
