# Test clearing the back buffer.

def RenderFrame(context):
	# Clear the lighting buffer (i.e. the back buffer).
	# Don't use 0.5 as it can round up or down depending on GPU!
	context.Clear((0.502, 0.502, 1.0, 1))

ri.SetFrameCallback(RenderFrame)
