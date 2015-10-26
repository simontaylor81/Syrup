from SRPScripting import *

vs = ri.CompileShader("pbr.hlsl", "BasicVS", "vs_4_0")

ps_ibl = ri.CompileShader("pbr.hlsl", "SolidColourPS", "ps_4_0", { 'PBR_USE_IBL': 1 })
ps_noibl = ri.CompileShader("pbr.hlsl", "SolidColourPS", "ps_4_0", { 'PBR_USE_IBL': 0 })

backgroundVs = ri.CompileShader("background.hlsl", "VS", "vs_4_0")
backgroundPs = ri.CompileShader("background.hlsl", "PS", "ps_4_0")

pixelshaders = [ps_ibl, ps_noibl]
ri.BindShaderVariableToMaterial(pixelshaders, "BaseColour", "BaseColour")
ri.BindShaderVariableToMaterial(pixelshaders, "PbrParams", "PbrParams")

useIbl = ri.AddUserVar("Use IBL", UserVariableType.Bool, True)

# Load environment cubemap.
envmap = ri.LoadTexture("assets/Arches_E_PineTree_Cube.dds")
ri.SetShaderResourceVariable(backgroundPs, "EnvCube", envmap)
ri.SetShaderResourceVariable(ps_ibl, "EnvCube", envmap)
ri.SetShaderResourceVariable(ps_noibl, "EnvCube", envmap)

def RenderFrame(context):
	context.Clear((0.5, 0.5, 1.0, 0))
	context.DrawFullscreenQuad(backgroundVs, backgroundPs)
	context.DrawScene(vs, ps_ibl if useIbl() else ps_noibl)

ri.SetFrameCallback(RenderFrame)
