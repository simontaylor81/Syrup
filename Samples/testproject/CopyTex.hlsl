// Shaders for simply copying one texture to the render target.

Texture2D tex;

SamplerState mySampler
{
    Filter = MIN_MAG_MIP_NEAREST;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct PSIn
{
	float4 Pos	: SV_Position;
	float2 UV	: TEXCOORD0;
};

PSIn CopyTex_VS(float4 Pos : POSITION)
{
	PSIn Result;
	Result.Pos = Pos;

	Result.UV = Pos.xy * 0.5 + 0.5;
	Result.UV.y = 1 - Result.UV.y;
	
	return Result;
}

float4 CopyTex_PS(PSIn In) : SV_Target
{
	return tex.Sample(mySampler, In.UV);
	//return float4(1, 0, 0, 1);
}
