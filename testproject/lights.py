from SRPScripting import *
import random

vs = ri.CompileShader("Lights.hlsl", "VSMain", "vs_4_0")
ps = ri.CompileShader("Lights.hlsl", "ForwardLightPS", "ps_4_0")

copytexVS = ri.CompileShader("CopyTex.hlsl", "CopyTex_VS", "vs_4_0")
copytexPS = ri.CompileShader("CopyTex.hlsl", "CopyTex_PS", "ps_4_0")

rt = ri.CreateRenderTarget()

ri.BindShaderVariable(vs, "WorldToProjectionMatrix", ShaderVariableBindSource.WorldToProjectionMatrix)
ri.BindShaderVariable(vs, "LocalToWorldMatrix", ShaderVariableBindSource.LocalToWorldMatrix)
ri.BindShaderVariable(ps, "CameraPos", ShaderVariableBindSource.CameraPosition);

ri.SetShaderResourceVariable(copytexPS, "tex", rt)

# Expose radius rather than inverse-square-radius.
radius = ri.AddUserVar("Radius", UserVariableType.Float, 20)
def InvSqrRadius():
	r = max(0.0001, radius())
	return 1.0 / (r*r)
ri.SetShaderVariable(ps, "LightInvSqrRadius", InvSqrRadius)

wireframe = ri.AddUserVar("Wireframe?", UserVariableType.Bool, False)

ri.BindShaderResourceToMaterial(ps, "DiffuseTex", "DiffuseTexture")


def RenderFrame(context):
	context.Clear((0.5, 0.5, 1.0, 0), [rt])
	context.DrawScene(
		vs,
		ps,
		renderTargets = [rt],
		rastState = RastState(fillMode = FillMode.Wireframe if wireframe() else FillMode.Solid))
	context.DrawFullscreenQuad(copytexVS, copytexPS)

ri.SetFrameCallback(RenderFrame)

# TEST
dict = {'a': 40, 'c': 23}
list = [8, 9, 7]
#ri.Args(**dict)

#state = RastState(fillMode = FillMode.Wireframe,
#				  depthBias = 22,
#				  enableScissor = True)
#ri.ComplexArg(state)

