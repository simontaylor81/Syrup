// Test for indirect compute shader dispatch
// This is a special compute-only test script. It relies on stuff
// from the test framework so will not work inside Syrup itself.
using SRPScripting;
using System.Linq;

var cs = ri.CompileShader("ComputeTest.hlsl", "IndirectDispatchTest", "cs_4_0");

// Create indirect dispatch buffer.
// Deliberately don't spawn enough threads so we can
// check that we executed exactly the right amount.
var dispatchCount = 12;
var argBuffer = ri.CreateBuffer(new[] { dispatchCount, 1, 1 })
	.ContainsDrawIndirectArgs();

int numElements = 16;
var buffer = ri.CreateUninitialisedBuffer(numElements * 4, 4);
cs.FindUavVariable("OutUAV").Set(buffer.CreateUav());

var expected = Enumerable.Range(0, dispatchCount)
	.Concat(Enumerable.Repeat(0, numElements - dispatchCount))
	.Cast<object>();

dynamic SetExpected = _testParams["SetExpected"];
dynamic SetResultBuffer = _testParams["SetResultBuffer"];

SetExpected(expected);
SetResultBuffer(buffer);

ri.SetFrameCallback(context =>
{
	context.DispatchIndirect(cs, argBuffer, 0);
});
