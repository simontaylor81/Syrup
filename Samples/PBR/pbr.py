from SRPScripting import *

vs = ri.LoadShader("pbr.hlsl", "BasicVS", "vs_4_0")
ps = ri.LoadShader("pbr.hlsl", "SolidColourPS", "ps_4_0")

backgroundVs = ri.LoadShader("background.hlsl", "VS", "vs_4_0")
backgroundPs = ri.LoadShader("background.hlsl", "PS", "ps_4_0")

ri.BindShaderVariableToMaterial(ps, "SolidColour", "DiffuseColour")

# Load environment cubemap.
envmap = ri.LoadTexture("assets/Arches_E_PineTree_Cube.dds")
ri.SetShaderResourceVariable(backgroundPs, "EnvCube", envmap)
ri.SetShaderResourceVariable(ps, "EnvCube", envmap)

def RenderFrame(context):
	context.Clear((0.5, 0.5, 1.0, 0))
	context.DrawFullscreenQuad(backgroundVs, backgroundPs)
	context.DrawScene(vs, ps)

ri.SetFrameCallback(RenderFrame)
