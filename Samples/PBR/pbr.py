from SRPScripting import *

useIblVar = ri.AddUserVar_Bool("Use IBL", True)
useIbl = 1 if useIblVar() else 0

vs = ri.CompileShader("pbr.hlsl", "BasicVS", "vs_4_0")
ps = ri.CompileShader("pbr.hlsl", "SolidColourPS", "ps_4_0", { 'PBR_USE_IBL': useIbl })

backgroundVs = ri.CompileShader("background.hlsl", "VS", "vs_4_0")
backgroundPs = ri.CompileShader("background.hlsl", "PS", "ps_4_0")

# Load environment cubemap.
envmap = ri.LoadTexture("assets/Arches_E_PineTree_Cube.dds")

ps.FindConstantVariable("BaseColour").BindToMaterial("BaseColour")
ps.FindConstantVariable("PbrParams").BindToMaterial("PbrParams")

ps.FindResourceVariable("BaseColourTex").BindToMaterial("BaseColour", fallback = ri.WhiteTexture);
ps.FindResourceVariable("SmoothnessTex").BindToMaterial("Smoothness", fallback = ri.WhiteTexture);
ps.FindResourceVariable("MetallicTex").BindToMaterial("Metallic", fallback = ri.WhiteTexture);
ps.FindResourceVariable("NormalTex").BindToMaterial("Normal", fallback = ri.DefaultNormalTexture);

ps.FindResourceVariable("EnvCube").Set(envmap);
backgroundPs.FindResourceVariable("EnvCube").Set(envmap);

def RenderFrame(context):
	context.Clear((0.5, 0.5, 1.0, 0))
	context.DrawFullscreenQuad(backgroundVs, backgroundPs)
	context.DrawScene(vs, ps)

ri.SetFrameCallback(RenderFrame)
