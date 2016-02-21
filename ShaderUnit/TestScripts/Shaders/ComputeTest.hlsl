// Very simple shaders writing constant colour.

RWStructuredBuffer<float> OutUAV;
StructuredBuffer<float> InBuffer;

[numthreads(16, 1, 1)]
void WriteToUAV(uint3 id : SV_DispatchThreadID)
{
	OutUAV[id.x] = 2.0f * id.x + 10.0f;
}

[numthreads(16, 1, 1)]
void ReadFromBuffer(uint3 id : SV_DispatchThreadID)
{
	OutUAV[id.x] = 2.0f * InBuffer[id.x];
}
