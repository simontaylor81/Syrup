// Test for reading from a buffer in a compute shader.
// This is a special compute-only test script. It relies on stuff
// from the test framework so will not work inside Syrup itself.
using SRPScripting;
using System.Linq;

var cs = ri.CompileShader("ComputeTest.hlsl", "ReadFromBuffer", "cs_4_0");

int numElements = 16;
var input = Enumerable.Range(0, numElements).Select(x => (float)x);

var inputBuffer = ri.CreateStructuredBuffer(input);
var outputBuffer = ri.CreateUninitialisedBuffer(numElements * 4, 4);
cs.FindResourceVariable("InBuffer").Set(inputBuffer);
cs.FindUavVariable("OutUAV").Set(outputBuffer.CreateUav());

var expected = Enumerable.Range(0, numElements)
	.Select(i => 2.0f * i + 12.0f)
	.Cast<object>();

dynamic SetExpected = _testParams["SetExpected"];
dynamic SetResultBuffer = _testParams["SetResultBuffer"];

SetExpected(expected);
SetResultBuffer(outputBuffer);

ri.SetFrameCallback(context =>
{
	context.Dispatch(cs, 1, 1, 1);
});
