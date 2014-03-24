from SRPScripting import *
import random

# Compile shaders for base and deferred passes.
basepassVS = ri.LoadShader("DeferredShading.hlsl", "BasePass_VS", "vs_4_0")
basepassPS = ri.LoadShader("DeferredShading.hlsl", "BasePass_PS", "ps_4_0")
deferredVS = ri.LoadShader("DeferredShading.hlsl", "DeferredPass_VS", "vs_4_0")
deferredPS = ri.LoadShader("DeferredShading.hlsl", "DeferredPass_PS", "ps_4_0")

# Create g-buffer render targets.
albedoRT = ri.CreateRenderTarget()
normalRT = ri.CreateRenderTarget()

# Bind material textures.
ri.BindShaderResourceToMaterial(basepassPS, "DiffuseTex", "DiffuseTexture")

ri.ShaderVariableIsScriptOverride(deferredPS, "LightColour")
ri.ShaderVariableIsScriptOverride(deferredPS, "LightPos")
ri.ShaderVariableIsScriptOverride(deferredVS, "vsLightPos")

# Bind g-buffer render targets to variables for the deferred pass.
ri.SetShaderResourceVariable(deferredPS, "GBuffer_Albedo", albedoRT)
ri.SetShaderResourceVariable(deferredPS, "GBuffer_Normal", normalRT)
ri.SetShaderResourceVariable(deferredPS, "GBuffer_Depth", ri.DepthBuffer)

# Expose radius rather than inverse-square-radius.
radius = ri.AddUserVar("Radius", UserVariableType.Float, 20)
def InvSqrRadius():
	r = max(0.0001, radius())
	return 1.0 / (r*r)
ri.SetShaderVariable(deferredPS, "LightInvSqrRadius", InvSqrRadius)
ri.SetShaderVariable(deferredVS, "vsLightRadius", radius)

showlights = ri.AddUserVar("Show lights?", UserVariableType.Bool, False)


# Build a list of light positions and colours.
lightPositions = []
lightColours = []
for z in xrange(0, 4):
	for x in xrange(0, 4):
		lightPositions.append((5.0 * x - 10.0, 1.0, 5.0 * z - 10.0))
		lightColours.append((random.random(), random.random(), random.random()))


def RenderFrame(context):
	# Clear the lighting buffer (i.e. the back buffer).
	context.Clear((0.5, 0.5, 1.0, 0))

	# Draw the scene to fill the g-buffer.
	context.DrawScene(
		basepassVS,
		basepassPS,
		renderTargets = [None, normalRT, albedoRT])

	# Now each light.
	for pos, col in zip(lightPositions, lightColours):
		varOverrides = {
			"LightPos": pos,
			"vsLightPos": pos,
			"LightColour": col
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
		for pos, col in zip(lightPositions, lightColours):
			context.DrawWireSphere(
				pos,
				radius(),
				col
				)


ri.SetFrameCallback(RenderFrame)

