# Script for testing generating a texture based on a user property.

from SRPScripting import *
import random

vs = ri.CompileShader("BasicShaders.hlsl", "BasicVS", "vs_4_0")
ps = ri.CompileShader("BasicShaders.hlsl", "TexturedPS", "ps_4_0")
ps.FindSamplerVariable("mySampler").Set(SamplerState.PointWrap)

texColour = ri.AddUserVar_Float4("Colour", (1, 0.25, 0, 1))

#texColourA = (1, 0.75, 0, 1)	# TODO: get from user prop
texColourA = texColour()
texColourB = (texColourA[0] * 0.75, texColourA[1] * 0.75, texColourA[2] * 0.75, 1)

# Create a checkerboard texture
checker = [(x%2 + (x/16)%2)%2 for x in xrange(0, 16*16)]
texContents = [(texColourA if x != 0 else texColourB) for x in checker]
tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, texContents)

ps.FindResourceVariable("DiffuseTex").Set(tex)

def RenderFrame(context):
	context.DrawScene(vs, ps)

ri.SetFrameCallback(RenderFrame)
