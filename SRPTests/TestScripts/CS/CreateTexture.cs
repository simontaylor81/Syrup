// Test texture generation
#load "Utils/utils.cs"

// Create the texture.
var ramp = Enumerable.Range(0, 16*16).Select(i => new Vector4((i % 16) / 15.0f, (i / 16) / 15.0f, 0, 1));
var tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, ramp);

TestTexture(tex);
