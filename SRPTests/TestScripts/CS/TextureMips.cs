// Test texture mipmap generation
#load "Utils/utils.cs"

var mip = TestSetting<long>("mip", 1);
TestTextureFileLevel("Assets/Textures/ThisIsATest.png", mip);
