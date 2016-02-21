// Very simple shaders writing constant colour.

RWStructuredBuffer<float> OutUAV;
StructuredBuffer<float> InBuffer;

struct BufferElement
{
	float2 Vec2;
	uint Uint;
};
StructuredBuffer<BufferElement> InBufferComplex;

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

[numthreads(16, 1, 1)]
void ReadFromComplexBuffer(uint3 id : SV_DispatchThreadID)
{
	BufferElement element = InBufferComplex[id.x];
	OutUAV[id.x] = element.Vec2.x + element.Vec2.y + element.Uint;
}
