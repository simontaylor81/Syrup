from SRPScripting import *

vs = ri.CompileShader("pbr.hlsl", "BasicVS", "vs_4_0")

ps_ibl = ri.CompileShader("pbr.hlsl", "SolidColourPS", "ps_4_0", { 'PBR_USE_IBL': 1 })
ps_noibl = ri.CompileShader("pbr.hlsl", "SolidColourPS", "ps_4_0", { 'PBR_USE_IBL': 0 })

# TEMP: Compile loads of shaders to test parallel compilation.
#for i in xrange(0, 50):
#	ri.CompileShader("pbr.hlsl", "SolidColourPS", "ps_4_0", { 'PBR_USE_IBL': 1, 'DUMMY': i })

backgroundVs = ri.CompileShader("background.hlsl", "VS", "vs_4_0")
backgroundPs = ri.CompileShader("background.hlsl", "PS", "ps_4_0")

# Load environment cubemap.
envmap = ri.LoadTexture("assets/Arches_E_PineTree_Cube.dds")

def BindPixelShaderVars(ps):
	ps.FindConstantVariable("BaseColour").BindToMaterial("BaseColour")
	ps.FindConstantVariable("PbrParams").BindToMaterial("PbrParams")

	ps.FindResourceVariable("BaseColourTex").BindToMaterial("BaseColour", fallback = ri.WhiteTexture);
	ps.FindResourceVariable("SmoothnessTex").BindToMaterial("Smoothness", fallback = ri.WhiteTexture);
	ps.FindResourceVariable("MetallicTex").BindToMaterial("Metallic", fallback = ri.WhiteTexture);
	ps.FindResourceVariable("NormalTex").BindToMaterial("Normal", fallback = ri.DefaultNormalTexture);
	
	ps.FindResourceVariable("EnvCube").Set(envmap);

	
BindPixelShaderVars(ps_ibl)
BindPixelShaderVars(ps_noibl)
backgroundPs.FindResourceVariable("EnvCube").Set(envmap);

useIbl = ri.AddUserVar_Bool("Use IBL", True)

def RenderFrame(context):
	context.Clear((0.5, 0.5, 1.0, 0))
	context.DrawFullscreenQuad(backgroundVs, backgroundPs)
	context.DrawScene(vs, ps_ibl if useIbl() else ps_noibl)

ri.SetFrameCallback(RenderFrame)
