// Very simple shaders writing constant colour.

RWStructuredBuffer<float> OutUAV;

[numthreads(16, 1, 1)]
void Main(uint3 id : SV_DispatchThreadID)
{
	OutUAV[id.x] = 2.0f * id.x + 10.0f;
}
