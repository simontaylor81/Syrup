# Texture testing helper.
def TestTexture(tex, ri):
	# Compile shaders.
	vs = ri.LoadShader("FullscreenTexture.hlsl", "FullscreenTexture_VS", "vs_4_0")
	ps = ri.LoadShader("FullscreenTexture.hlsl", "FullscreenTexture_PS", "ps_4_0")
	
	ri.SetShaderResourceVariable(ps, "tex", tex)
	
	def RenderFrame(context):
		# Clear the lighting buffer (i.e. the back buffer).
		context.DrawFullscreenQuad(vs, ps)
	
	ri.SetFrameCallback(RenderFrame)