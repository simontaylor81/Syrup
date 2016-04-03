// Test very simple pixel shader.

var vs = ri.CompileShader("ConstantColour.hlsl", "VS", "vs_4_0");
var ps = ri.CompileShader("ConstantColour.hlsl", "PS", "ps_4_0");

ri.SetFrameCallback(context =>
{
	context.DrawFullscreenQuad(vs, ps);
});
