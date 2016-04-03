// Test texture generation
#load "Utils/utils.cs"

// Create the texture.
object getPixel(int x, int y)
{
	return new Vector4(x / 15.0f, y / 15.0f, 0, 1);
}
	
var tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, getPixel);
TestTexture(tex);
