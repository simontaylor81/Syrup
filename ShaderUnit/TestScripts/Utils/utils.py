from SRPScripting import *

# Register a test setting with a default value.
# Basically, this checks if we have a global variable,
# and if not add one with the default value. Allows test scripts
# with inputs from the test system to still be run in the editor.
def TestSetting(key, default, globalsDict):
	if key not in globalsDict:
		globalsDict[key] = default


# Texture testing helper.
def TestTexture_Impl(ri, tex, psEntryPoint, texShaderVar, level = None):
	# Compile shaders.
	vs = ri.CompileShader("FullscreenTexture.hlsl", "FullscreenTexture_VS", "vs_4_0")
	ps = ri.CompileShader("FullscreenTexture.hlsl", psEntryPoint, "ps_4_0")
	
	ri.SetShaderResourceVariable(ps, texShaderVar, tex)
	ri.SetShaderSamplerState(ps, "mySampler", SamplerState.PointClamp)
	
	if level != None:
		ri.SetShaderVariable(ps, "MipLevel", level)
	
	def RenderFrame(context):
		# Draw the texture fullscreen.
		context.DrawFullscreenQuad(vs, ps)
	
	ri.SetFrameCallback(RenderFrame)
	
def TestTexture(ri, tex):
	TestTexture_Impl(ri, tex, "FullscreenTexture_PS", "tex")
	
def TestCubemap(ri, tex):
	TestTexture_Impl(ri, tex, "FullscreenTextureCube_PS", "texCube")

def TestTextureLevel(ri, tex, level):
	TestTexture_Impl(ri, tex, "FullscreenTextureLevel_PS", "tex", level)
	
def TestCubemapLevel(ri, tex, level):
	TestTexture_Impl(ri, tex, "FullscreenTextureCubeLevel_PS", "texCube", level)


# Test texture loading from file.
def TestTextureFile(ri, filename):
	tex = ri.LoadTexture(filename)
	TestTexture(ri, tex)

# Test texture mip-map generation by rendering a specific level.
def TestTextureFileLevel(ri, filename, level):
	tex = ri.LoadTexture(filename)
	TestTextureLevel(ri, tex, level)

# Test cubemap texture loading from file.
def TestCubemapFile(ri, filename):
	tex = ri.LoadTexture(filename)
	TestCubemap(ri, tex)

# Test cubemap texture loading from file.
def TestCubemapFileLevel(ri, filename, level):
	tex = ri.LoadTexture(filename)
	TestCubemapLevel(ri, tex, level)
