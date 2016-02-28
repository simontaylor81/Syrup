using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D11;
using System.Numerics;

namespace SRPRendering
{
	// Class for rendering an overlay on top of the viewport.
	internal class OverlayRenderer
	{
		private readonly IGlobalResources _globalResources;

		public OverlayRenderer(IGlobalResources globalResources)
		{
			_globalResources = globalResources;
		}

		// Render the overlay.
		public void Draw(DeviceContext deviceContext, RenderScene scene, ViewInfo viewInfo)
		{
			if (scene == null || selectedMeshIndex < 0 || selectedMeshIndex >= scene.Primitives.Count())
			{
				return;
			}

			// Set wireframe render state. Use the script state types for convenience.
			var rastState = new SRPScripting.RastState(SRPScripting.FillMode.Wireframe, SRPScripting.CullMode.None, 0, -0.5f);
			var depthState = SRPScripting.DepthStencilState.DisableDepthWrite;
			var blendState = SRPScripting.BlendState.NoBlending;

			deviceContext.Rasterizer.State = _globalResources.RastStateCache.Get(rastState.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = _globalResources.DepthStencilStateCache.Get(depthState.ToD3D11());
			deviceContext.OutputMerger.BlendState = _globalResources.BlendStateCache.Get(blendState.ToD3D11());

			// Set simple shaders.
			_globalResources.BasicShaders.BasicSceneVS.Set(deviceContext);
			_globalResources.BasicShaders.SolidColourPS.Set(deviceContext);

			// Set shader constants.
			_globalResources.BasicShaders.SolidColourShaderVar.Set(new Vector4(1.0f, 1.0f, 0.0f, 1.0f));	// Yellow

			// Set input layout
			deviceContext.InputAssembler.InputLayout = _globalResources.InputLayoutCache.GetInputLayout(
				deviceContext.Device, _globalResources.BasicShaders.BasicSceneVS.Signature, InputLayoutCache.SceneVertexInputElements);

			// Draw the selected mesh
			var proxy = scene.Primitives.ElementAt(selectedMeshIndex);

			_globalResources.BasicShaders.BasicSceneVS.UpdateVariables(deviceContext, viewInfo, proxy, null, _globalResources);
			_globalResources.BasicShaders.SolidColourPS.UpdateVariables(deviceContext, viewInfo, proxy, null, _globalResources);
			proxy.Mesh.Draw(deviceContext);
		}

		// TEMP
		private static int selectedMeshIndex = -1;
	}
}
