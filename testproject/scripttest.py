from SRPScripting import *
import random

vs = ri.CompileShader("BasicShaders.hlsl", "BasicVS", "vs_4_0")
ps = ri.CompileShader("BasicShaders.hlsl", "SolidColourPS", "ps_4_0")
psTex = ri.CompileShader("BasicShaders.hlsl", "TexturedPS", "ps_4_0")

def func(x, y):
	return (random.random(), random.random(), random.random(), random.random())

#Test: create a checkerboard texture
checker = [(x%2 + (x/16)%2)%2 for x in xrange(0, 16*16)]
texContents = [(1, x, 0, 1) for x in checker]
ramp = [((i % 16) / 15.0, (i / 16) / 15.0, 0, 1) for i in xrange(0, 16*16)]
tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, ramp)

ri.SetShaderResourceVariable(psTex, "DiffuseTex", tex)


dummyVar = ri.AddUserVar_Float4("FloatUserVar", (0,1,2,45))


def errorfunc():
	x = 10
	x.Method()
	return 0.5
	
def funcWithArgs(x, y):
	return x + y
	
def GetFillMode():
	return FillMode.Solid

#ri.SetShaderVariable(ps, "SolidColour", func)
ri.BindShaderVariableToMaterial(ps, "SolidColour", "DiffuseColour")

def RenderFrame(context):
	rastState = RastState(fillMode = GetFillMode(), cullMode = CullMode.None)
	#context.DrawSphere(vs, psTex, rastState)
	context.DrawScene(vs, psTex, rastState)

ri.SetFrameCallback(RenderFrame)

