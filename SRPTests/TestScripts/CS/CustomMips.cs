// Test custom mipmap generation.
#load "Utils/utils.cs"

var sampler = TestSetting<string>("sampler", "linear");
var mip = TestSetting<long>("mip", 1);
var filename = "CustomDownsample_2D_" + sampler + ".hlsl";
var tex = ri.LoadTexture("Assets/Textures/ThisIsATest.png")
	.WithCustomMips(filename);

TestTextureLevel(tex, mip);
