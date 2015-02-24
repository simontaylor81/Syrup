def RenderFrame(context):
	# Clear the lighting buffer (i.e. the back buffer).
	context.Clear((0.5, 0.5, 1.0, 1))

ri.SetFrameCallback(RenderFrame)
