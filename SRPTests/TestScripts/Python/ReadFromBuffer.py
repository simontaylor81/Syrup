# Test for reading from a buffer in a compute shader.
# This is a special compute-only test script. It relies on stuff
# from the test framework so will not work inside Syrup itself.
from SRPScripting import *

cs = ri.CompileShader("ComputeTest.hlsl", "ReadFromBuffer", "cs_4_0")

numElements = 16
input = range(0, numElements)

inputBuffer = ri.CreateBuffer(numElements, Format.R32_Float, input)
outputBuffer = ri.CreateBuffer(numElements, Format.R32_Float, None)
cs.FindResourceVariable("InBuffer").Set(inputBuffer)
cs.FindUavVariable("OutUAV").Set(outputBuffer.CreateUav())

expected = [2.0 * i + 12.0 for i in xrange(0, numElements)]

SetExpected(expected)
SetResultBuffer(outputBuffer)

def RenderFrame(context):
	context.Dispatch(cs, 1, 1, 1)

ri.SetFrameCallback(RenderFrame)
