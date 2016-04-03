from SRPScripting import *

vs = ri.CompileShader("Lights.hlsl", "VSMain", "vs_4_0")
ps = ri.CompileShader("Lights.hlsl", "ForwardLightPS", "ps_4_0")

copytexVS = ri.CompileShader("CopyTex.hlsl", "CopyTex_VS", "vs_4_0")
copytexPS = ri.CompileShader("CopyTex.hlsl", "CopyTex_PS", "ps_4_0")

rt = ri.CreateRenderTarget()

ps.FindConstantVariable("CameraPos").Bind(ShaderConstantVariableBindSource.CameraPosition)

copytexPS.FindResourceVariable("tex").Set(rt)

# Expose radius rather than inverse-square-radius.
radius = ri.AddUserVar_Float("Radius", 20)
def InvSqrRadius():
	r = max(0.0001, radius())
	return 1.0 / (r*r)
ps.FindConstantVariable("LightInvSqrRadius").Set(InvSqrRadius)

wireframe = ri.AddUserVar_Bool("Wireframe?", False)

ps.FindResourceVariable("DiffuseTex").BindToMaterial("DiffuseTexture")


def RenderFrame(context):
	context.Clear((0.5, 0.5, 1.0, 0), [rt])
	context.DrawScene(
		vs,
		ps,
		renderTargets = [rt],
		rastState = RastState(fillMode = FillMode.Wireframe if wireframe() else FillMode.Solid))
	context.DrawFullscreenQuad(copytexVS, copytexPS)

ri.SetFrameCallback(RenderFrame)
