from SRPScripting import *

defines = { 'PBR_USE_IBL': 1 }

vs = ri.CompileShader("pbr.hlsl", "BasicVS", "vs_4_0", defines)
ps = ri.CompileShader("pbr.hlsl", "SolidColourPS", "ps_4_0", defines)

backgroundVs = ri.CompileShader("background.hlsl", "VS", "vs_4_0")
backgroundPs = ri.CompileShader("background.hlsl", "PS", "ps_4_0")

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
