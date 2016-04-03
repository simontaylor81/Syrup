using SRPScripting;

// Get a test setting with a default value.
// Allows test scripts with inputs from the test system to still be run in the editor.
T TestSetting<T>(string key, T def)
{
	object result;
	if (_testParams.TryGetValue(key, out result))
	{
		return (T)result;
	}
	return def;
}

// Texture testing helper.
// We use long for the level param as that's what comes out of the test params (it's a json thing).
FrameCallback GetTestTextureCallback(IShaderResource tex, string psEntryPoint, string texShaderVar, long? level = null)
{
	// Compile shaders.
	var vs = ri.CompileShader("FullscreenTexture.hlsl", "FullscreenTexture_VS", "vs_4_0");
	var ps = ri.CompileShader("FullscreenTexture.hlsl", psEntryPoint, "ps_4_0");
	
	ps.FindResourceVariable(texShaderVar).Set(tex);
	ps.FindSamplerVariable("mySampler").Set(SamplerState.PointClamp);
	
	if (level.HasValue)
	{
		ps.FindConstantVariable("MipLevel").Set((int)level);
	}
	
	return context =>
	{
		// Draw the texture fullscreen.
		context.DrawFullscreenQuad(vs, ps);
	};
}

void TestTexture_Impl(IShaderResource tex, string psEntryPoint, string texShaderVar, long? level = null)
{
	ri.SetFrameCallback(GetTestTextureCallback(tex, psEntryPoint, texShaderVar, level));
}
	
void TestTexture(IShaderResource tex)
{
	TestTexture_Impl(tex, "FullscreenTexture_PS", "tex");
}

void TestCubemap(IShaderResource tex)
{
	TestTexture_Impl(tex, "FullscreenTextureCube_PS", "texCube");
}

void TestTextureLevel(IShaderResource tex, long level)
{
	TestTexture_Impl(tex, "FullscreenTextureLevel_PS", "tex", level);
}
	
void TestCubemapLevel(IShaderResource tex, long level)
{
	TestTexture_Impl(tex, "FullscreenTextureCubeLevel_PS", "texCube", level);
}

// Test texture loading from file.
void TestTextureFile(string filename)
{
	var tex = ri.LoadTexture(filename);
	TestTexture(tex);
}

// Test texture mip-map generation by rendering a specific level.
void TestTextureFileLevel(string filename, long level)
{
	var tex = ri.LoadTexture(filename);
	TestTextureLevel(tex, level);
}

// Test cubemap texture loading from file.
void TestCubemapFile(string filename)
{
	var tex = ri.LoadTexture(filename);
	TestCubemap(tex);
}

// Test cubemap texture loading from file.
void TestCubemapFileLevel(string filename, long level)
{
	var tex = ri.LoadTexture(filename);
	TestCubemapLevel(tex, level);
}
