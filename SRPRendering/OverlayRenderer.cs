using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX.Direct3D11;
using SlimDX;
using SRPCommon.Scene;

namespace SRPRendering
{
	// Class for rendering an overlay on top of the viewport.
	internal class OverlayRenderer
	{
		private readonly BasicShaders _basicShaders;
		private readonly IGlobalResources _globalResources;

		public OverlayRenderer(BasicShaders basicShaders, IGlobalResources globalResources)
		{
			this._basicShaders = basicShaders;
			_globalResources = globalResources;
		}

		// Render the overlay.
		public void Draw(DeviceContext deviceContext, RenderScene scene, ViewInfo viewInfo)
		{
			if (scene == null || selectedMeshIndex < 0 || selectedMeshIndex >= scene.PrimitiveProxies.Count())
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
			_basicShaders.BasicSceneVS.Set(deviceContext);
			_basicShaders.SolidColourPS.Set(deviceContext);

			// Set shader constants.
			_basicShaders.SolidColour = new Color4(1.0f, 1.0f, 0.0f);	// Yellow

			// Set input layout
			deviceContext.InputAssembler.InputLayout = _globalResources.InputLayoutCache.GetInputLayout(
				deviceContext.Device, _basicShaders.BasicSceneVS.Signature, SceneVertex.InputElements);

			// Draw the selected mesh
			var proxy = scene.PrimitiveProxies.ElementAt(selectedMeshIndex);

			_basicShaders.BasicSceneVS.UpdateVariables(deviceContext, viewInfo, proxy, null, _globalResources);
			_basicShaders.SolidColourPS.UpdateVariables(deviceContext, viewInfo, proxy, null, _globalResources);
			proxy.Mesh.Draw(deviceContext);
		}

		// TEMP
		private static int selectedMeshIndex = -1;
	}
}
