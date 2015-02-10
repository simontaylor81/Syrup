using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SRPScripting;
using SRPCommon.Scripting;
using SRPCommon.Scene;

namespace SRPRendering
{
	class ScriptRenderContext : IRenderContext
	{
		public ScriptRenderContext(DeviceContext deviceContext,
								   ViewInfo viewInfo,
								   RenderScene scene,
								   IList<Shader> shaders,
								   IList<RenderTarget> renderTargets,
								   Mesh sphereMesh,
								   FullscreenQuad fullscreenQuad,
								   BasicShaders basicShaders)
		{
			this.deviceContext = deviceContext;
			this.viewInfo = viewInfo;
			this.scene = scene;
			this.shaders = shaders;
			this.renderTargetResources = renderTargets;
			this.sphereMesh = sphereMesh;
			this.fullscreenQuad = fullscreenQuad;
			this.basicShaders = basicShaders;
		}

		#region IRenderContext interface

		// Draw the entire scene.
		public void DrawScene(dynamic vertexShaderIndex,
							  dynamic pixelShaderIndex,
							  RastState rastState = null,
							  SRPScripting.DepthStencilState depthStencilState = null,
							  SRPScripting.BlendState blendState = null,
							  IEnumerable<object> renderTargetHandles = null,
							  object depthBuffer = null,
							  IDictionary<string, dynamic> shaderVariableOverrides = null)
		{
			Shader vertexShader = GetShader(vertexShaderIndex);
			Shader pixelShader = GetShader(pixelShaderIndex);
			
			// Vertex shader is not optional.
			if (vertexShader == null)
				throw new ScriptException("DrawScene: Cannot draw without a vertex shader.");

			// Must have a scene to draw one!
			if (scene == null)
				throw new ScriptException("DrawScene: No scene set.");

			// Set input layout
			deviceContext.InputAssembler.InputLayout = GlobalResources.Instance.InputLayoutCache.GetInputLayout(
				deviceContext.Device, vertexShader.Signature, SceneVertex.InputElements);

			// Set render state.
			SetRenderTargets(renderTargetHandles, depthBuffer);
			deviceContext.Rasterizer.State = GlobalResources.Instance.RastStateCache.Get(rastState.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = GlobalResources.Instance.DepthStencilStateCache.Get(depthStencilState.ToD3D11());
			deviceContext.OutputMerger.BlendState = GlobalResources.Instance.BlendStateCache.Get(blendState.ToD3D11());
			SetShaders(vertexShader, pixelShader);

			// Draw each mesh.
			foreach (var proxy in scene.PrimitiveProxies)
			{
				UpdateShaders(vertexShader, pixelShader, proxy, shaderVariableOverrides);
				proxy.Mesh.Draw(deviceContext);
			}

			// Force all state to defaults -- we're completely stateless.
			deviceContext.ClearState();
		}

		// Draw a unit sphere.
		public void DrawSphere(dynamic vertexShaderIndex,
							   dynamic pixelShaderIndex,
							   RastState rastState = null,
							   SRPScripting.DepthStencilState depthStencilState = null,
							   SRPScripting.BlendState blendState = null,
							   IEnumerable<object> renderTargetHandles = null,
							   object depthBuffer = null,
							   IDictionary<string, dynamic> shaderVariableOverrides = null)
		{
			Shader vertexShader = GetShader(vertexShaderIndex);
			Shader pixelShader = GetShader(pixelShaderIndex);

			// Vertex shader is not optional.
			if (vertexShader == null)
				throw new ScriptException("DrawSphere: Cannot draw without a vertex shader.");

			// Set input layout
			deviceContext.InputAssembler.InputLayout = GlobalResources.Instance.InputLayoutCache.GetInputLayout(
				deviceContext.Device, vertexShader.Signature, sphereMesh.InputElements);

			// Set render state.
			SetRenderTargets(renderTargetHandles, depthBuffer);
			deviceContext.Rasterizer.State = GlobalResources.Instance.RastStateCache.Get(rastState.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = GlobalResources.Instance.DepthStencilStateCache.Get(depthStencilState.ToD3D11());
			deviceContext.OutputMerger.BlendState = GlobalResources.Instance.BlendStateCache.Get(blendState.ToD3D11());
			SetShaders(vertexShader, pixelShader);

			// Draw the sphere mesh.
			UpdateShaders(vertexShader, pixelShader, null, shaderVariableOverrides);		// TODO: Pass a valid proxy here?
			sphereMesh.Draw(deviceContext);

			// Force all state to defaults -- we're completely stateless.
			deviceContext.ClearState();
		}

		// Draw a fullscreen quad.
		public void DrawFullscreenQuad(dynamic vertexShaderIndex,
									   dynamic pixelShaderIndex,
									   IEnumerable<object> renderTargetHandles = null,
									   IDictionary<string, dynamic> shaderVariableOverrides = null)
		{
			Shader vertexShader = GetShader(vertexShaderIndex);
			Shader pixelShader = GetShader(pixelShaderIndex);

			// Vertex shader is not optional.
			if (vertexShader == null)
				throw new ScriptException("DrawFullscreenQuad: Cannot draw without a vertex shader.");

			// Set input layout
			deviceContext.InputAssembler.InputLayout = GlobalResources.Instance.InputLayoutCache.GetInputLayout(
				deviceContext.Device, vertexShader.Signature, FullscreenQuad.InputElements);

			// Set render state.
			SetRenderTargets(renderTargetHandles, DepthBufferHandle.NoDepthBuffer);
			deviceContext.Rasterizer.State = GlobalResources.Instance.RastStateCache.Get(RastState.Default.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = GlobalResources.Instance.DepthStencilStateCache.Get(SRPScripting.DepthStencilState.DisableDepth.ToD3D11());
			deviceContext.OutputMerger.BlendState = GlobalResources.Instance.BlendStateCache.Get(SRPScripting.BlendState.NoBlending.ToD3D11());
			SetShaders(vertexShader, pixelShader);

			// Draw the quad.
			UpdateShaders(vertexShader, pixelShader, null, shaderVariableOverrides);
			fullscreenQuad.Draw(deviceContext);

			// Force all state to defaults -- we're completely stateless.
			deviceContext.ClearState();
		}

		// Clear render targets.
		public void Clear(dynamic colour, IEnumerable<object> renderTargetHandles = null)
		{
			// Convert list of floats to a colour.
			try
			{
				var col = ScriptHelper.ConvertToColor4(colour);

				// Clear each specified target.
				var rtvs = GetRTVS(renderTargetHandles);
				foreach (var rtv in rtvs)
				{
					deviceContext.ClearRenderTargetView(rtv, col);
				}
			}
			catch (ScriptException ex)
			{
				throw new ScriptException("Clear: Invalid colour.", ex);
			}
		}

		// Draw a wireframe sphere.
		public void DrawWireSphere(dynamic position,
								   float radius,
								   dynamic colour,
								   object renderTarget = null)
		{
			try
			{
				// Convert position and colour to a real vector and colour.
				var pos = ScriptHelper.ConvertToVector3(position);
				var col = new Color4(ScriptHelper.ConvertToColor3(colour));

				// Set render target.
				SetRenderTargets(new[] { renderTarget }, null);

				// Set wireframe render state.
				var rastState = new RastState(SRPScripting.FillMode.Wireframe);
				deviceContext.Rasterizer.State = GlobalResources.Instance.RastStateCache.Get(rastState.ToD3D11());
				deviceContext.OutputMerger.DepthStencilState = GlobalResources.Instance.DepthStencilStateCache.Get(SRPScripting.DepthStencilState.DisableDepthWrite.ToD3D11());
				deviceContext.OutputMerger.BlendState = GlobalResources.Instance.BlendStateCache.Get(SRPScripting.BlendState.NoBlending.ToD3D11());

				// Set simple shaders.
				SetShaders(basicShaders.BasicSceneVS, basicShaders.SolidColourPS);

				// Construct transform matrix.
				var transform = Matrix.Scaling(radius, radius, radius) * Matrix.Translation(pos);

				// Set shader constants.
				basicShaders.SolidColour = col;
				UpdateShaders(basicShaders.BasicSceneVS, basicShaders.SolidColourPS, new SimplePrimitiveProxy(transform), null);

				// Set input layout
				deviceContext.InputAssembler.InputLayout = GlobalResources.Instance.InputLayoutCache.GetInputLayout(
					deviceContext.Device, basicShaders.BasicSceneVS.Signature, sphereMesh.InputElements);

				// Draw the sphere.
				sphereMesh.Draw(deviceContext);
			}
			catch (ScriptException ex)
			{
				throw new ScriptException("Invalid parameters to DrawWireSphere.", ex);
			}
			finally
			{
				// Force all state to defaults -- we're completely stateless.
				deviceContext.ClearState();
			}
		}

		#endregion

		// Access a shader by handle.
		private Shader GetShader(dynamic handle, [CallerMemberName] string caller = null)
		{
			// null means no shader.
			if (handle == null)
				return null;

			// If it's not null, but not a valid index, throw.
			if (handle.index < 0 || handle.index >= shaders.Count)
				throw new ScriptException(string.Format("Invalid shader given to {0}.", caller));

			return shaders[handle.index];
		}

		// Access a render target by handle.
		private RenderTarget GetRenderTarget(object handleObj, [CallerMemberName] string caller = null)
		{
			var handle = handleObj as RenderTargetHandle;

			// If it's null or not a valid index, throw.
			if (handle == null || handle.index < 0 || handle.index >= shaders.Count)
				throw new ScriptException(string.Format("Invalid render target given to {0}.", caller));

			return renderTargetResources[handle.index];
		}

		// Set the given shaders to the device.
		private void SetShaders(params Shader[] shaders)
		{
			foreach (var shader in shaders)
			{
				if (shader != null)
					shader.Set(deviceContext);
			}
		}

		// Update the variables of the given shaders, unless they're null.
		private void UpdateShaders(Shader vs, Shader ps, IPrimitive primitive, IDictionary<string, dynamic> variableOverrides)
		{
			if (vs != null)
				vs.UpdateVariables(deviceContext, viewInfo, primitive, variableOverrides);
			else
				deviceContext.VertexShader.Set(null);

			if (ps != null)
				ps.UpdateVariables(deviceContext, viewInfo, primitive, variableOverrides);
			else
				deviceContext.PixelShader.Set(null);
		}

		// Set render targets based on the given list of handles.
		private void SetRenderTargets(IEnumerable<object> renderTargetHandles, object depthBuffer)
		{
			// Collect render target views for the given handles.
			var rtvs = GetRTVS(renderTargetHandles).ToArray();

			// Find the depth buffer.
			DepthStencilView dsv;
			if (depthBuffer == null || DepthBufferHandle.Default.Equals(depthBuffer))
			{
				dsv = viewInfo.DepthBuffer.DSV;
			}
			else if (DepthBufferHandle.NoDepthBuffer.Equals(depthBuffer))
			{
				dsv = null;
			}
			else
			{
				// TODO: User-allocated depth buffers.
				throw new ScriptException("Invalid depth buffer.");
			}

			// Set them to the device.
			deviceContext.OutputMerger.SetTargets(dsv, rtvs);

			// Set viewport.
			deviceContext.Rasterizer.SetViewports(GetViewport(renderTargetHandles));
		}

		// Converts a list of render target handles to a list of RTVs, resolving nulls to the back buffer.
		private IEnumerable<RenderTargetView> GetRTVS(IEnumerable<object> renderTargetHandles)
		{
			if (renderTargetHandles != null)
			{
				return from handle in renderTargetHandles
					   select handle != null ? GetRenderTarget(handle).RTV : viewInfo.BackBuffer;
			}
			else
			{
				// No render targets specified, so write to backbuffer.
				return new[] { viewInfo.BackBuffer };
			}
		}

		// Get the viewport dimensions to use.
		private Viewport GetViewport(IEnumerable<object> renderTargetHandles)
		{
			if (renderTargetHandles != null)
			{
				// Get viewport size from the first render target.
				var handle = renderTargetHandles.FirstOrDefault();
				if (handle != null)
				{
					var rt = GetRenderTarget(handle);
					new Viewport(0.0f, 0.0f, rt.Width, rt.Height);
				}
			}

			return new Viewport(0.0f, 0.0f, viewInfo.ViewportWidth, viewInfo.ViewportHeight);
		}

		private DeviceContext deviceContext;
		private RenderScene scene;
		private IList<Shader> shaders;
		private IList<RenderTarget> renderTargetResources;
		private ViewInfo viewInfo;
		private Mesh sphereMesh;
		private FullscreenQuad fullscreenQuad;
		private BasicShaders basicShaders;
	}
}
