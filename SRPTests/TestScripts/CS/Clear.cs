// Test clearing the back buffer.
using System.Numerics;

ri.SetFrameCallback(context =>
{
	// Clear the lighting buffer (i.e. the back buffer).
	// Don't use 0.5 as it can round up or down depending on GPU!
	context.Clear(new Vector4(0.502f, 0.502f, 1.0f, 1.0f));
});
