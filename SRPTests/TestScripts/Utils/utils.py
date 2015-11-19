# Texture testing helper.
def TestTexture(tex, ri):
	# Compile shaders.
	vs = ri.CompileShader("FullscreenTexture.hlsl", "FullscreenTexture_VS", "vs_4_0")
	ps = ri.CompileShader("FullscreenTexture.hlsl", "FullscreenTexture_PS", "ps_4_0")
	
	ri.SetShaderResourceVariable(ps, "tex", tex)
	
	def RenderFrame(context):
		# Draw the texture fullscreen.
		context.DrawFullscreenQuad(vs, ps)
	
	ri.SetFrameCallback(RenderFrame)


# Test texture loading from file.
def TestTextureFile(filename, ri):
	tex = ri.LoadTexture(filename)
	TestTexture(tex, ri)
