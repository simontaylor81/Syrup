
float3 BaseColour = float3(0.4, 0.5, 0.1);
//float Smoothness = 0.5;
//float Metalic = 0.0f;
//float Reflectance = 0.5;
// (Smoothness, Metalic, Reflectance)
float3 PbrParams = float3(0.5, 0, 0.5);

float3 GetDiffuseAlbedo()
{
	return lerp(BaseColour, 0, PbrParams.y);
}

float GetSmoothness()
{
	return PbrParams.x;
}

float3 GetF0()
{
	return lerp(PbrParams.z * 0.08, BaseColour, PbrParams.y);
}
