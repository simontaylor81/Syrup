// Test custom cubemap mipmap generation.
#load "Utils/utils.cs"

var mip = TestSetting<long>("mip", 1);
var filename = "CustomDownsample_Cube.hlsl";
var tex = ri.LoadTexture("Assets/Textures/Cubemap.dds")
	.WithCustomMips(filename);

TestCubemapLevel(tex, mip);
