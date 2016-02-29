using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPScripting;

namespace SRPRendering
{
	class DeferredRenderContext : IRenderContext
	{
		private List<Action<DeviceContext>> _commands = new List<Action<DeviceContext>>();
		private HashSet<Shader> _shaders = new HashSet<Shader>();

		// All the shaders in use this frame.
		public IEnumerable<Shader> Shaders => _shaders;

		public DeferredRenderContext(
			ViewInfo viewInfo,
			RenderScene scene,
			IList<IShader> shaders,
			IList<RenderTarget> renderTargets,
			IGlobalResources globalResources)
		{
			this.viewInfo = viewInfo;
			this.scene = scene;
			this.shaders = shaders;
			this.renderTargetResources = renderTargets;
			_globalResources = globalResources;
		}

		// TODO: Pass in deviceContext here instead of constructor.
		public void Execute(DeviceContext deviceContext)
		{
			foreach (var command in _commands)
			{
				command(deviceContext);
			}
		}

		#region IRenderContext interface

		public void DrawScene(dynamic vertexShaderIndex, dynamic pixelShaderIndex, RastState rastState = null, SRPScripting.DepthStencilState depthStencilState = null, SRPScripting.BlendState blendState = null, IEnumerable<object> renderTargets = null, object depthBuffer = null, IDictionary<string, object> shaderVariableOverrides = null)
		{
			Shader vertexShader = GetShader(vertexShaderIndex);
			Shader pixelShader = GetShader(pixelShaderIndex);

			// Vertex shader is not optional.
			if (vertexShader == null)
				throw new ScriptException("DrawScene: Cannot draw without a vertex shader.");

			// Must have a scene to draw one!
			if (scene == null)
				throw new ScriptException("DrawScene: No scene set.");

			_shaders.Add(vertexShader);
			if (pixelShader != null)
				_shaders.Add(pixelShader);

			_commands.Add(deviceContext => DrawSceneImpl(deviceContext, vertexShader, pixelShader, rastState,
				depthStencilState, blendState, renderTargets, depthBuffer, shaderVariableOverrides));
		}

		public void DrawSphere(dynamic vertexShaderIndex, dynamic pixelShaderIndex, RastState rastState = null, SRPScripting.DepthStencilState depthStencilState = null, SRPScripting.BlendState blendState = null, IEnumerable<object> renderTargets = null, object depthBuffer = null, IDictionary<string, object> shaderVariableOverrides = null)
		{
			Shader vertexShader = GetShader(vertexShaderIndex);
			Shader pixelShader = GetShader(pixelShaderIndex);

			// Vertex shader is not optional.
			if (vertexShader == null)
				throw new ScriptException("DrawSphere: Cannot draw without a vertex shader.");

			_shaders.Add(vertexShader);
			if (pixelShader != null)
				_shaders.Add(pixelShader);

			_commands.Add(deviceContext => DrawSphereImpl(deviceContext, vertexShader, pixelShader, rastState,
				depthStencilState, blendState, renderTargets, depthBuffer, shaderVariableOverrides));
		}

		public void DrawFullscreenQuad(dynamic vertexShaderIndex, dynamic pixelShaderIndex, IEnumerable<object> renderTargets = null, IDictionary<string, object> shaderVariableOverrides = null)
		{
			Shader vertexShader = GetShader(vertexShaderIndex);
			Shader pixelShader = GetShader(pixelShaderIndex);

			// Vertex shader is not optional.
			if (vertexShader == null)
				throw new ScriptException("DrawFullscreenQuad: Cannot draw without a vertex shader.");

			_shaders.Add(vertexShader);
			if (pixelShader != null)
				_shaders.Add(pixelShader);

			_commands.Add(deviceContext => DrawFullscreenQuadImpl(deviceContext, vertexShader, pixelShader, renderTargets, shaderVariableOverrides));
		}

		public void Dispatch(dynamic shader, int numGroupsX, int numGroupsY, int numGroupsZ, IDictionary<string, object> shaderVariableOverrides = null)
		{
			Shader cs = GetShader(shader);
			if (cs == null)
			{
				throw new ScriptException("Dispatch: compute shader is required");
			}

			_shaders.Add(cs);
			_commands.Add(deviceContext => DispatchImpl(deviceContext, cs, numGroupsX, numGroupsY, numGroupsZ, shaderVariableOverrides));
		}

		public void Clear(dynamic colour, IEnumerable<object> renderTargets = null)
		{
			// Convert list of floats to a colour.
			try
			{
				Vector4 vectorColour = ScriptHelper.ConvertToVector4(colour);
				var rawColour = new RawColor4(vectorColour.X, vectorColour.Y, vectorColour.Z, vectorColour.W);

				_commands.Add(deviceContext => ClearImpl(deviceContext, rawColour, renderTargets));
			}
			catch (ScriptException ex)
			{
				throw new ScriptException("Clear: Invalid colour.", ex);
			}
		}

		public void DrawWireSphere(dynamic position, float radius, dynamic colour, object renderTarget = null)
		{
			try
			{
				// Convert position and colour to a real vector and colour.
				var pos = ScriptHelper.ConvertToVector3(position);
				var col = new Vector4(ScriptHelper.ConvertToVector3(colour), 1.0f);

				_commands.Add(deviceContext => DrawWireSphereImpl(deviceContext, pos, radius, col, renderTarget));
			}
			catch (ScriptException ex)
			{
				throw new ScriptException("Invalid parameters to DrawWireSphere.", ex);
			}
		}

		#endregion

		#region Actual rendering implementation

		// Draw the entire scene.
		private void DrawSceneImpl(
			DeviceContext deviceContext,
			Shader vertexShader,
			Shader pixelShader,
			RastState rastState,
			SRPScripting.DepthStencilState depthStencilState,
			SRPScripting.BlendState blendState,
			IEnumerable<object> renderTargetHandles,
			object depthBuffer,
			IDictionary<string, object> shaderVariableOverrides)
		{
			// Set input layout
			deviceContext.InputAssembler.InputLayout = _globalResources.InputLayoutCache.GetInputLayout(
				deviceContext.Device, vertexShader.Signature, InputLayoutCache.SceneVertexInputElements);

			// Set render state.
			SetRenderTargets(deviceContext, renderTargetHandles, depthBuffer);
			deviceContext.Rasterizer.State = _globalResources.RastStateCache.Get(rastState.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = _globalResources.DepthStencilStateCache.Get(depthStencilState.ToD3D11());
			deviceContext.OutputMerger.BlendState = _globalResources.BlendStateCache.Get(blendState.ToD3D11());
			SetShaders(deviceContext, vertexShader, pixelShader);

			// Draw each mesh.
			foreach (var proxy in scene.Primitives)
			{
				UpdateShaders(deviceContext, vertexShader, pixelShader, proxy, shaderVariableOverrides);
				proxy.Mesh.Draw(deviceContext);
			}

			// Force all state to defaults -- we're completely stateless.
			deviceContext.ClearState();
		}

		// Draw a unit sphere.
		private void DrawSphereImpl(
			DeviceContext deviceContext,
			Shader vertexShader,
			Shader pixelShader,
			RastState rastState,
			SRPScripting.DepthStencilState depthStencilState,
			SRPScripting.BlendState blendState,
			IEnumerable<object> renderTargetHandles,
			object depthBuffer,
			IDictionary<string, object> shaderVariableOverrides)
		{
			// Set input layout
			deviceContext.InputAssembler.InputLayout = _globalResources.InputLayoutCache.GetInputLayout(
				deviceContext.Device, vertexShader.Signature, BasicMesh.InputElements);

			// Set render state.
			SetRenderTargets(deviceContext, renderTargetHandles, depthBuffer);
			deviceContext.Rasterizer.State = _globalResources.RastStateCache.Get(rastState.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = _globalResources.DepthStencilStateCache.Get(depthStencilState.ToD3D11());
			deviceContext.OutputMerger.BlendState = _globalResources.BlendStateCache.Get(blendState.ToD3D11());
			SetShaders(deviceContext, vertexShader, pixelShader);

			// Draw the sphere mesh.
			UpdateShaders(deviceContext, vertexShader, pixelShader, null, shaderVariableOverrides);        // TODO: Pass a valid proxy here?
			_globalResources.SphereMesh.Draw(deviceContext);

			// Force all state to defaults -- we're completely stateless.
			deviceContext.ClearState();
		}

		// Draw a fullscreen quad.
		private void DrawFullscreenQuadImpl(
			DeviceContext deviceContext,
			Shader vertexShader,
			Shader pixelShader,
			IEnumerable<object> renderTargetHandles,
			IDictionary<string, object> shaderVariableOverrides)
		{
			// Set input layout
			deviceContext.InputAssembler.InputLayout = _globalResources.InputLayoutCache.GetInputLayout(
				deviceContext.Device, vertexShader.Signature, FullscreenQuad.InputElements);

			// Set render state.
			SetRenderTargets(deviceContext, renderTargetHandles, DepthBufferHandle.NoDepthBuffer);
			deviceContext.Rasterizer.State = _globalResources.RastStateCache.Get(RastState.Default.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = _globalResources.DepthStencilStateCache.Get(SRPScripting.DepthStencilState.DisableDepth.ToD3D11());
			deviceContext.OutputMerger.BlendState = _globalResources.BlendStateCache.Get(SRPScripting.BlendState.NoBlending.ToD3D11());
			SetShaders(deviceContext, vertexShader, pixelShader);

			// Draw the quad.
			UpdateShaders(deviceContext, vertexShader, pixelShader, null, shaderVariableOverrides);
			_globalResources.FullscreenQuad.Draw(deviceContext);

			// Force all state to defaults -- we're completely stateless.
			deviceContext.ClearState();
		}

		// Dispatch a compute shader.
		private void DispatchImpl(
			DeviceContext deviceContext,
			Shader cs,
			int numGroupsX,
			int numGroupsY,
			int numGroupsZ,
			IDictionary<string, object> shaderVariableOverrides)
		{
			cs.Set(deviceContext);
			cs.UpdateVariables(deviceContext, viewInfo, null, shaderVariableOverrides, _globalResources);
			deviceContext.Dispatch(numGroupsX, numGroupsY, numGroupsZ);

			// Enforce statelessness.
			deviceContext.ClearState();
		}

		// Clear render targets.
		private void ClearImpl(DeviceContext deviceContext, RawColor4 colour, IEnumerable<object> renderTargetHandles)
		{
			// Clear each specified target.
			var rtvs = GetRTVS(renderTargetHandles);
			foreach (var rtv in rtvs)
			{
				deviceContext.ClearRenderTargetView(rtv, colour);
			}
		}

		// Draw a wireframe sphere.
		private void DrawWireSphereImpl(
			DeviceContext deviceContext,
			Vector3 position,
			float radius,
			Vector4 colour,
			object renderTarget)
		{
			// Set render target.
			SetRenderTargets(deviceContext, new[] { renderTarget }, null);

			// Set wireframe render state.
			var rastState = new RastState(SRPScripting.FillMode.Wireframe);
			deviceContext.Rasterizer.State = _globalResources.RastStateCache.Get(rastState.ToD3D11());
			deviceContext.OutputMerger.DepthStencilState = _globalResources.DepthStencilStateCache.Get(SRPScripting.DepthStencilState.DisableDepthWrite.ToD3D11());
			deviceContext.OutputMerger.BlendState = _globalResources.BlendStateCache.Get(SRPScripting.BlendState.NoBlending.ToD3D11());

			// Set simple shaders.
			SetShaders(deviceContext, _globalResources.BasicShaders.BasicSceneVS, _globalResources.BasicShaders.SolidColourPS);

			// Construct transform matrix.
			var transform = Matrix4x4.CreateScale(radius, radius, radius) * Matrix4x4.CreateTranslation(position);

			// Set shader constants.
			_globalResources.BasicShaders.SolidColourShaderVar.Set(colour);
			UpdateShaders(deviceContext, _globalResources.BasicShaders.BasicSceneVS, _globalResources.BasicShaders.SolidColourPS, new SimplePrimitiveProxy(transform), null);

			// Set input layout
			deviceContext.InputAssembler.InputLayout = _globalResources.InputLayoutCache.GetInputLayout(
				deviceContext.Device, _globalResources.BasicShaders.BasicSceneVS.Signature, BasicMesh.InputElements);

			// Draw the sphere.
			_globalResources.SphereMesh.Draw(deviceContext);

			// Force all state to defaults -- we're completely stateless.
			deviceContext.ClearState();
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
		private void SetShaders(DeviceContext deviceContext, params IShader[] shaders)
		{
			foreach (var shader in shaders)
			{
				if (shader != null)
					shader.Set(deviceContext);
			}
		}

		// Update the variables of the given shaders, unless they're null.
		private void UpdateShaders(DeviceContext deviceContext, IShader vs, IShader ps, IPrimitive primitive, IDictionary<string, object> variableOverrides)
		{
			if (vs != null)
				vs.UpdateVariables(deviceContext, viewInfo, primitive, variableOverrides, _globalResources);
			else
				deviceContext.VertexShader.Set(null);

			if (ps != null)
				ps.UpdateVariables(deviceContext, viewInfo, primitive, variableOverrides, _globalResources);
			else
				deviceContext.PixelShader.Set(null);
		}

		// Set render targets based on the given list of handles.
		private void SetRenderTargets(DeviceContext deviceContext, IEnumerable<object> renderTargetHandles, object depthBuffer)
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
			deviceContext.Rasterizer.SetViewports(new[] { GetViewport(renderTargetHandles) });
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
		private RawViewportF GetViewport(IEnumerable<object> renderTargetHandles)
		{
			if (renderTargetHandles != null)
			{
				// Get viewport size from the first render target.
				var handle = renderTargetHandles.FirstOrDefault();
				if (handle != null)
				{
					var rt = GetRenderTarget(handle);
					return new RawViewportF
					{
						X = 0.0f,
						Y = 0.0f,
						Width = rt.Width,
						Height = rt.Height,
						MinDepth = 0.0f,
						MaxDepth = 1.0f,
					};
				}
			}

			return new RawViewportF
			{
				X = 0.0f,
				Y = 0.0f,
				Width = viewInfo.ViewportWidth,
				Height = viewInfo.ViewportHeight,
				MinDepth = 0.0f,
				MaxDepth = 1.0f,
			};
		}

		private RenderScene scene;
		private IList<IShader> shaders;
		private IList<RenderTarget> renderTargetResources;
		private ViewInfo viewInfo;
		private IGlobalResources _globalResources;
	}
}
