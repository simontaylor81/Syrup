using System;
using SRPScripting;
using System.Numerics;

var vs = ri.CompileShader("Lights.hlsl", "VSMain", "vs_4_0");
var ps = ri.CompileShader("Lights.hlsl", "ForwardLightPS", "ps_4_0");

var copytexVS = ri.CompileShader("CopyTex.hlsl", "CopyTex_VS", "vs_4_0");
var copytexPS = ri.CompileShader("CopyTex.hlsl", "CopyTex_PS", "ps_4_0");

var rt = ri.CreateRenderTarget();

ps.FindConstantVariable("CameraPos").Bind(ShaderConstantVariableBindSource.CameraPosition);

copytexPS.FindResourceVariable("tex").Set(rt);

// Expose radius rather than inverse-square-radius.
var radius = ri.AddUserVar_Float("Radius", 20);
float InvSqrRadius()
{
	float r = Math.Max(0.0001f, radius());
	return 1.0f / (r*r);
}
ps.FindConstantVariable("LightInvSqrRadius").Set((Func<float>)InvSqrRadius);

var wireframe = ri.AddUserVar_Bool("Wireframe?", false);

ps.FindResourceVariable("DiffuseTex").BindToMaterial("DiffuseTexture");


void RenderFrame(IRenderContext context)
{
	context.Clear(new Vector4(0.5f, 0.5f, 1.0f, 0), new[] {rt});
	context.DrawScene(
		vs,
		ps,
		renderTargets: new[] {rt},
		rastState: new RastState(fillMode: wireframe() ? FillMode.Wireframe : FillMode.Solid));
	context.DrawFullscreenQuad(copytexVS, copytexPS);
}

ri.SetFrameCallback(RenderFrame);
