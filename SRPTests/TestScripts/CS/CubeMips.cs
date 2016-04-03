// Test cubemap mipmap generation
#load "Utils/utils.cs"

var mip = TestSetting<long>("mip", 3);
TestCubemapFileLevel("Assets/Textures/Cubemap.dds", mip);
