# Test very simple pixel shader.

vs = ri.CompileShader("ConstantColour.hlsl", "VS", "vs_4_0")
ps = ri.CompileShader("ConstantColour.hlsl", "PS", "ps_4_0")

def RenderFrame(context):
	context.DrawFullscreenQuad(vs, ps)

ri.SetFrameCallback(RenderFrame)
