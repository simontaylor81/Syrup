// Various functions to be unit tested.

float NoParams_ReturnFloat()
{
	return 12.0f;
}
int NoParams_ReturnInt()
{
	return 57;
}
float2 NoParams_ReturnFloat2()
{
	return float2(11.0f, 12.0f);
}
float3 NoParams_ReturnFloat3()
{
	return float3(11.0f, 12.0f, 13.0f);
}
float4 NoParams_ReturnFloat4()
{
	return float4(11.0f, 12.0f, 13.0f, 14.0f);
}

float OneFloatParam(float x)
{
	return x + 1.0f;
}
float TwoFloatParams(float x, float y)
{
	return x + y;
}
float OneFloat2Param(float2 v)
{
	return dot(v, v);
}
float OneFloat3Param(float3 v)
{
	return dot(v, v);
}
float OneFloat4Param(float4 v)
{
	return dot(v, v);
}
