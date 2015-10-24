// Utility functions, etc.

#define PI 3.141265

float Sqr(float x)
{
	return x * x;
}

// Based on http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
// With jitter as per UE4
float2 Hammersley(uint index, uint N, uint2 random)
{
	return float2(
		frac((float)index / N + float(random.x & 0xffff) / (1<<16)),
		float(reversebits(index) ^ random.y) * 2.3283064365386963e-10);
}

// Munge 2 integers based on Tiny Encryption Algorithm
// http://citeseer.ist.psu.edu/viewdoc/download?doi=10.1.1.45.281&rep=rep1&type=pdf
// http://www.csee.umbc.edu/~olano/papers/GPUTEA.pdf
uint2 ScrambleTEA(uint2 seed)
{
	uint numIterations = 3;
	uint delta = 0x9e3779b9;	// from original TEA paper
	uint k[4] = { 0xA341316C, 0xC8013EA4, 0xAD90777D , 0x7E95761E };	// From Zafar et al.
	uint sum = 0;
	
	for (uint i = 0; i < numIterations; i++)
	{
		sum += delta;
		seed.x += ((seed.y << 4) + k[0]) ^ (seed.y + sum) ^ ((seed.y >> 5) + k[1]);
		seed.y += ((seed.x << 4) + k[2]) ^ (seed.x + sum) ^ ((seed.x >> 5) + k[3]);
	}
	
	return seed;
}
