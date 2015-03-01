# Test very simple pixel shader.

vs = ri.LoadShader("ConstantColour.hlsl", "VS", "vs_4_0")
ps = ri.LoadShader("ConstantColour.hlsl", "PS", "ps_4_0")

def RenderFrame(context):
	context.DrawFullscreenQuad(vs, ps)

ri.SetFrameCallback(RenderFrame)
