from SRPScripting import *

vs = ri.LoadShader("pbr.hlsl", "BasicVS", "vs_4_0")
ps = ri.LoadShader("pbr.hlsl", "SolidColourPS", "ps_4_0")

ri.BindShaderVariableToMaterial(ps, "SolidColour", "DiffuseColour")

def RenderFrame(context):
	context.Clear((0.5, 0.5, 1.0, 0))
	context.DrawScene(vs, ps)

ri.SetFrameCallback(RenderFrame)
