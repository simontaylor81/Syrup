# Test for writing to a UAV from a compute shader.
# This is a special compute-only test script. It relies on stuff
# from the test framework so will not work inside Syrup itself.
from SRPScripting import *

cs = ri.CompileShader("ComputeTest.hlsl", "WriteToUAV", "cs_4_0")

numElements = 16
buffer = ri.CreateBuffer(numElements, Format.R32_Float, None)
cs.FindUavVariable("OutUAV").Set(buffer.CreateUav())

expected = [2.0 * i + 10.0 for i in xrange(0, numElements)]

SetExpected(expected)
SetResultBuffer(buffer)

def RenderFrame(context):
	context.Dispatch(cs, 1, 1, 1)

ri.SetFrameCallback(RenderFrame)
