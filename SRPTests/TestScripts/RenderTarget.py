# Test for creating custom render targets.

from SRPScripting import *
import utils

rt = ri.CreateRenderTarget()

testTexCallback = utils.GetTestTextureCallback(ri, rt, "FullscreenTexture_PS", "tex")

def RenderFrame(context):
	context.Clear((1, 0.5, 0, 1), [rt])
	testTexCallback()
	
ri.SetFrameCallback(RenderFrame)
