from SRPScripting import *8
import random

# Compile shaders for base and deferred passes.
basepassVS = ri.CompileShader("DeferredShading.hlsl", "BasePass_VS", "vs_4_0")
basepassPS = ri.CompileShader("DeferredShading.hlsl", "BasePass_PS", "ps_4_0")
deferredVS = ri.CompileShader("DeferredShading.hlsl", "DeferredPass_VS", "vs_4_0")
deferredPS = ri.CompileShader("DeferredShading.hlsl", "DeferredPass_PS", "ps_4_0")

# Create g-buffer render targets.
albedoRT = ri.CreateRenderTarget()
normalRT = ri.CreateRenderTarget()

# Bind material textures.
basepassPS.FindResourceVariable("DiffuseTex").BindToMaterial("DiffuseTexture")

deferredPS.FindConstantVariable("LightColour").MarkAsScriptOverride()
deferredPS.FindConstantVariable("LightPos").MarkAsScriptOverride()
deferredVS.FindConstantVariable("vsLightPos").MarkAsScriptOverride()
deferredPS.FindConstantVariable("LightInvSqrRadius").MarkAsScriptOverride()
deferredVS.FindConstantVariable("vsLightRadius").MarkAsScriptOverride()

# Bind g-buffer render targets to variables for the deferred pass.
deferredPS.FindResourceVariable("GBuffer_Albedo").Set(albedoRT)
deferredPS.FindResourceVariable("GBuffer_Normal").Set(normalRT)
deferredPS.FindResourceVariable("GBuffer_Depth").Set(ri.DepthBuffer)

# Little helper to convert radius to inverse-square-radius.
def InvSqrRadius(radius):
	r = max(0.0001, radius)
	return 1.0 / (r*r)

showlights = ri.AddUserVar_Bool("Show lights?", False)


# Build a list of light positions and colours.
lightPositions = []
lightColours = []
for z in xrange(0, 4):
	for x in xrange(0, 4):
		lightPositions.append((5.0 * x - 10.0, 1.0, 5.0 * z - 10.0))
		lightColours.append((random.random(), random.random(), random.random()))


# Get lights from scene
lights = ri.GetScene().Lights


def RenderFrame(context):
	# Clear the lighting buffer (i.e. the back buffer).
	context.Clear((0.5, 0.5, 1.0, 0))

	# Draw the scene to fill the g-buffer.
	context.DrawScene(
		basepassVS,
		basepassPS,
		renderTargets = [None, normalRT, albedoRT])

	# Now each light.
	for light in ri.GetScene().Lights:
		varOverrides = {
			"LightPos": light.position,
			"vsLightPos": light.position,
			"LightColour": light.colour,
			"LightInvSqrRadius": InvSqrRadius(light.radius),
			"vsLightRadius": light.radius
			}
			
		context.DrawSphere(
			deferredVS
			,deferredPS
			,depthBuffer = ri.NoDepthBuffer
			,rastState = RastState(fillMode = FillMode.Solid, cullMode = CullMode.Front)
			,depthStencilState = DepthStencilState.EqualDepth
			,blendState = BlendState.AdditiveBlending
			,shaderVariableOverrides = varOverrides
			)
			
	# Visualise light positions with wireframe spheres.
	if showlights():
		for light in ri.GetScene().Lights:
			context.DrawWireSphere(
				light.position,
				light.radius,
				light.colour)


ri.SetFrameCallback(RenderFrame)

