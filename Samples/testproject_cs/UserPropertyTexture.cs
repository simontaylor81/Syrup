// Script for testing generating a texture based on a user property.
var vs = ri.CompileShader("BasicShaders.hlsl", "BasicVS", "vs_4_0");
var ps = ri.CompileShader("BasicShaders.hlsl", "TexturedPS", "ps_4_0");
ps.FindSamplerVariable("mySampler").Set(SamplerState.PointWrap);

var texColour = ri.AddUserVar_Float4("Colour", new Vector4(1, 0.25f, 0, 1));

var texColourA = texColour();
var texColourB = new Vector4(texColourA[0] * 0.75f, texColourA[1] * 0.75f, texColourA[2] * 0.75f, 1);

// Create a checkerboard texture
var checker = Enumerable.Range(0, 16 * 16)
	.Select(x => (x % 2 + (x / 16) % 2) % 2)
	.Select(x => x != 0 ? texColourA : texColourB);
var tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, checker);

ps.FindResourceVariable("DiffuseTex").Set(tex);

void RenderFrame(IRenderContext context)
{
	context.DrawScene(vs, ps);
}

ri.SetFrameCallback(RenderFrame);
