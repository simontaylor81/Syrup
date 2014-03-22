from SRPScripting import *
import random

vs = ri.LoadShader("BasicShaders.hlsl", "SolidColourVS", "vs_4_0")
ps = ri.LoadShader("BasicShaders.hlsl", "SolidColourPS", "ps_4_0")
psTex = ri.LoadShader("BasicShaders.hlsl", "TexturedPS", "ps_4_0")

ri.BindShaderVariable(vs, "WorldToProjectionMatrix", ShaderVariableBindSource.WorldToProjectionMatrix)
ri.BindShaderVariable(vs, "LocalToWorldMatrix", ShaderVariableBindSource.LocalToWorldMatrix)

def func():
	return (random.random(), random.random(), random.random())

def errorfunc():
	x = 10
	x.Method()
	return 0.5
	
def funcWithArgs(x, y):
	return x + y
	
def GetFillMode():
	return FillMode.Wireframe

#ri.SetFillMode(GetFillMode)

#ri.SetShaderVariable(ps, "SolidColour", func)
ri.BindShaderVariableToMaterial(ps, "SolidColour", "DiffuseColour")
ri.BindShaderResourceToMaterial(psTex, "DiffuseTex", "DiffuseTexture")

#ri.SetShader(vs)

#ri.SetShader(psTex)
#ri.DrawScene()

#ri.SetShader(ps)
#ri.SetFillMode(FillMode.Wireframe)
#ri.DrawSphere()

def RenderFrame(context):
	rastState = RastState(fillMode = GetFillMode(), cullMode = CullMode.None)
	context.DrawSphere(vs, psTex, rastState)

ri.SetFrameCallback(RenderFrame)

