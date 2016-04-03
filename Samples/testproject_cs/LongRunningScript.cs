using SRPScripting;
using System.Threading;

var vs = ri.CompileShader("BasicShaders.hlsl", "BasicVS", "vs_4_0");
var ps = ri.CompileShader("BasicShaders.hlsl", "SolidColourPS", "ps_4_0");

ps.FindConstantVariable("SolidColour").BindToMaterial("DiffuseColour");

// Insert long delay to test async script execution.
Thread.Sleep(5000);

void RenderFrame(IRenderContext context)
{
	context.DrawScene(vs, ps);
}

ri.SetFrameCallback(RenderFrame);
