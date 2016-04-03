// Test for writing to a UAV from a compute shader.
// This is a special compute-only test script. It relies on stuff
// from the test framework so will not work inside Syrup itself.
using SRPScripting;
using System.Linq;

var cs = ri.CompileShader("ComputeTest.hlsl", "WriteToUAV", "cs_4_0");

int numElements = 16;
var buffer = ri.CreateUninitialisedBuffer(numElements * 4, 4);
cs.FindUavVariable("OutUAV").Set(buffer.CreateUav());

var expected = Enumerable.Range(0, numElements)
	.Select(i => 2.0f * i + 10.0f)
	.Cast<object>();

dynamic SetExpected = _testParams["SetExpected"];
dynamic SetResultBuffer = _testParams["SetResultBuffer"];

SetExpected(expected);
SetResultBuffer(buffer);

ri.SetFrameCallback(context =>
{
	context.Dispatch(cs, 1, 1, 1);
});
