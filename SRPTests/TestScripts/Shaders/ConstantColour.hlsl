// Very simple shaders writing constant colour.

float4 VS(float4 Pos : POSITION) : SV_Position
{
	return Pos;
}

float4 PS() : SV_Target
{
	return float4(1,0,0,1);
}
