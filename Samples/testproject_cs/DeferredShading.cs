// Compile shaders for base and deferred passes.
var basepassVS = ri.CompileShader("DeferredShading.hlsl", "BasePass_VS", "vs_4_0");
var basepassPS = ri.CompileShader("DeferredShading.hlsl", "BasePass_PS", "ps_4_0");
var deferredVS = ri.CompileShader("DeferredShading.hlsl", "DeferredPass_VS", "vs_4_0");
var deferredPS = ri.CompileShader("DeferredShading.hlsl", "DeferredPass_PS", "ps_4_0");

// Create g-buffer render targets.
var albedoRT = ri.CreateRenderTarget();
var normalRT = ri.CreateRenderTarget();

// Bind material textures.
basepassPS.FindResourceVariable("DiffuseTex").BindToMaterial("DiffuseTexture");

deferredPS.FindConstantVariable("LightColour").MarkAsScriptOverride();
deferredPS.FindConstantVariable("LightPos").MarkAsScriptOverride();
deferredVS.FindConstantVariable("vsLightPos").MarkAsScriptOverride();
deferredPS.FindConstantVariable("LightInvSqrRadius").MarkAsScriptOverride();
deferredVS.FindConstantVariable("vsLightRadius").MarkAsScriptOverride();

// Bind g-buffer render targets to variables for the deferred pass.
deferredPS.FindResourceVariable("GBuffer_Albedo").Set(albedoRT);
deferredPS.FindResourceVariable("GBuffer_Normal").Set(normalRT);
deferredPS.FindResourceVariable("GBuffer_Depth").Set(ri.DepthBuffer);

// Little helper to convert radius to inverse-square-radius.
float InvSqrRadius(float radius)
{
	float r = Math.Max(0.0001f, radius);
	return 1.0f / (r*r);
}

var showlights = ri.AddUserVar_Bool("Show lights?", false);


// Get lights from scene
var lights = ri.GetScene().Lights;


void RenderFrame(IRenderContext context)
{
	// Clear the lighting buffer (i.e. the back buffer).
	context.Clear(new Vector4(0.5f, 0.5f, 1.0f, 0));

	// Draw the scene to fill the g-buffer.
	context.DrawScene(
		basepassVS,
		basepassPS,
		renderTargets: new[] { null, normalRT, albedoRT });

	// Now each light.
	foreach (var light in lights)
	{
		var varOverrides = new Dictionary<string, object>
		{
			{ "LightPos", light.position },
			{ "vsLightPos", light.position },
			{ "LightColour", light.colour },
			{ "LightInvSqrRadius", InvSqrRadius(light.radius) },
			{ "vsLightRadius", light.radius },
		};
			
		context.DrawSphere(
			deferredVS
			,deferredPS
			,depthBuffer: ri.NoDepthBuffer
			,rastState: new RastState(fillMode: FillMode.Solid, cullMode: CullMode.Front)
			,depthStencilState: DepthStencilState.EqualDepth
			,blendState: BlendState.AdditiveBlending
			,shaderVariableOverrides: varOverrides
			);
	}
	
	// Visualise light positions with wireframe spheres.
	if (showlights())
	{
		foreach (var light in lights)
		{
			context.DrawWireSphere(
				light.position,
				light.radius,
				light.colour);
		}
	}
}

ri.SetFrameCallback(RenderFrame);

