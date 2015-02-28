# Test texture generation

from SRPScripting import *

# Create the texture.
ramp = [((i % 16) / 15.0, (i / 16) / 15.0, 0, 1) for i in xrange(0, 16*16)]
tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, ramp)

# Compile shaders.
vs = ri.LoadShader("FullscreenTexture.hlsl", "FullscreenTexture_VS", "vs_4_0")
ps = ri.LoadShader("FullscreenTexture.hlsl", "FullscreenTexture_PS", "ps_4_0")

ri.SetShaderResourceVariable(ps, "tex", tex)

def RenderFrame(context):
	# Clear the lighting buffer (i.e. the back buffer).
	context.DrawFullscreenQuad(vs, ps)

ri.SetFrameCallback(RenderFrame)
