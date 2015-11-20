# Compile shaders.
vs = ri.CompileShader("FullscreenTexture.hlsl", "FullscreenTexture_VS", "vs_4_0")
ps = ri.CompileShader("FullscreenTexture.hlsl", "FullscreenTextureCube_PS", "ps_4_0")

tex = ri.LoadTexture('Assets/Textures/Cubemap.dds')
ri.SetShaderResourceVariable(ps, "texCube", tex)

def RenderFrame(context):
	# Draw the texture fullscreen.
	context.DrawFullscreenQuad(vs, ps)

ri.SetFrameCallback(RenderFrame)
