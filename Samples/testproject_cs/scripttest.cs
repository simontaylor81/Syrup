var vs = ri.CompileShader("BasicShaders.hlsl", "BasicVS", "vs_4_0");
var ps = ri.CompileShader("BasicShaders.hlsl", "SolidColourPS", "ps_4_0");
var psTex = ri.CompileShader("BasicShaders.hlsl", "TexturedPS", "ps_4_0");

// Test: create a checkerboard texture
var checker = Enumerable.Range(0, 16 * 16)
	.Select(x => (x % 2 + (x / 16) % 2) % 2);
var texContents = checker
	.Select(x => new Vector4(1, x, 0, 1));
var ramp = Enumerable.Range(0, 16 * 16)
	.Select(i => new Vector4((i % 16) / 15.0f, (i / 16) / 15.0f, 0, 1));
var tex = ri.CreateTexture2D(16, 16, Format.R8G8B8A8_UNorm, ramp);

psTex.FindResourceVariable("DiffuseTex").Set(tex);


var dummyVar = ri.AddUserVar_Float4("FloatUserVar", new [] { 0,1,2,45 });

var choiceVar = ri.AddUserVar_Choice("A choice", new object [] {'A', 'B', 'C'}, 'A');

float errorfunc()
{
	var x = 10;
	//x.Method();
	return 0.5f;
}
	
int funcWithArgs(int x, int y)
{
	return x + y;
}
	
FillMode GetFillMode()
{
	return FillMode.Solid;
}

ri.Log("Hello from C#");
	
ps.FindConstantVariable("SolidColour").BindToMaterial("DiffuseColour");
//ps.FindConstantVariable("SolidColour").Set(func);

void RenderFrame(IRenderContext context)
{
	var rastState = new RastState(fillMode: GetFillMode(), cullMode: CullMode.None);
	if (choiceVar() == 'B')
		context.DrawSphere(vs, psTex, rastState);
	else if (choiceVar() == 'C')
		context.DrawScene(vs, ps, rastState);
	else
		context.DrawScene(vs, psTex, rastState);
}
		
ri.SetFrameCallback(RenderFrame);

