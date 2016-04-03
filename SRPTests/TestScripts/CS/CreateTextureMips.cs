// Test script-created texture mip generation
#load "Utils/utils.cs"
using SRPScripting;
using System.Numerics;

var generateMips = TestSetting<bool>("generateMips", true);

var col1 = new Vector4(0, 0, 0, 1);
var col2 = new Vector4(1, 1, 1, 1);

int w = 16;
int h = 16;

// Create the texture.
Vector4 getPixel(int x, int y)
{
	bool set = x+1 == y || (w - x - 1) == y;
	return set ? col1 : col2;
}
	
var tex = ri.CreateTexture2D(w, h, Format.R8G8B8A8_UNorm, getPixel)
	.WithMips(generateMips);

TestTextureLevel(tex, 1);
