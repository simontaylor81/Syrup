// Test for creating custom render targets.
#load "Utils/utils.cs"
using System.Numerics;

var rt = ri.CreateRenderTarget();

var testTexCallback = GetTestTextureCallback(rt, "FullscreenTexture_PS", "tex");

ri.SetFrameCallback(context =>
{
	context.Clear(new Vector4(1, 0.5f, 0, 1), new[] {rt});
	testTexCallback(context);
});
