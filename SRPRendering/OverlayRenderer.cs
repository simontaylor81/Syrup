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
		private BasicShaders basicShaders;

		public OverlayRenderer(BasicShaders basicShaders)
		{
			this.basicShaders = basicShaders;
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

			deviceContext.Rasterizer.State = GlobalResources.Instance.RastStateCache.Get(rastState.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = GlobalResources.Instance.DepthStencilStateCache.Get(depthState.ToD3D11());
			deviceContext.OutputMerger.BlendState = GlobalResources.Instance.BlendStateCache.Get(blendState.ToD3D11());

			// Set simple shaders.
			basicShaders.BasicSceneVS.Set(deviceContext);
			basicShaders.SolidColourPS.Set(deviceContext);

			// Set shader constants.
			basicShaders.SolidColour = new Color4(1.0f, 1.0f, 0.0f);	// Yellow

			// Set input layout
			deviceContext.InputAssembler.InputLayout = GlobalResources.Instance.InputLayoutCache.GetInputLayout(
				deviceContext.Device, basicShaders.BasicSceneVS.Signature, SceneVertex.InputElements);

			// Draw the selected mesh
			var proxy = scene.PrimitiveProxies.ElementAt(selectedMeshIndex);

			basicShaders.BasicSceneVS.UpdateVariables(deviceContext, viewInfo, proxy, null);
			basicShaders.SolidColourPS.UpdateVariables(deviceContext, viewInfo, proxy, null);
			proxy.Mesh.Draw(deviceContext);
		}

		// TEMP
		private static int selectedMeshIndex = -1;
	}
}
