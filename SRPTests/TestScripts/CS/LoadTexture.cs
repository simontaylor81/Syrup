// Test PNG texture load
#load "Utils/utils.cs"

var extension = TestSetting<string>("extension", "png");
TestTextureFile("Assets/Textures/ThisIsATest." + extension);
