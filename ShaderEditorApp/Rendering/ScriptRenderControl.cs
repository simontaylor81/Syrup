using System;
using System.Collections.Generic;
using System.Linq;

using SRPScripting;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;
using ShaderEditorApp.Workspace;
using ShaderEditorApp.ViewModel;
using System.IO;
using System.Collections.ObjectModel;

namespace ShaderEditorApp.Rendering
{
	// Class that takes commands from the script and controls the rendering.
	// Anything not part of the IRenderInterface is marked internal to avoid the script calling it.
	class ScriptRenderControl : IDisposable, IPropertySource
	{
		internal ScriptRenderControl(WorkspaceViewModel workspace, Device device)
		{
			this.workspace = workspace;
			this.device = device;

			sphereMesh = BasicMesh.CreateSphere(device, 12, 6);
			fullscreenQuad = new FullscreenQuad(device);
			shaderCache = new ShaderCache(device);

			ScriptInterface = new ScriptRenderInterface(this);

			basicShaders = new BasicShaders(device);
			disposables.Add(basicShaders);
		}

		internal void Reset()
		{
			frameCallback = null;

			// Clear shaders array. Don't need to dispose as they're held by the cache.
			shaders.Clear();
			properties.Clear();
			userVariables.Clear();

			// Clear render target descriptors and dispose the actual render targets.
			RenderUtils.DisposeList(renderTargets);

			// Reset output logger so warnings are written again.
			OutputLogger.Instance.ResetLogOnce();
		}

		internal void ExecutionComplete(bool bSuccess)
		{
			bScriptError = !bSuccess;

			// Add shader variables to the properties list.
			foreach (var shader in shaders)
			{
				foreach (var variable in shader.Variables)
				{
					// Don't add bound variables.
					// Don't even add them as read-only, as they're too slow to update every time we render the frame.
					if (variable.Bind == null)
					{
						properties.Add(ShaderVariable.CreateViewModel(variable));
					}
				}
			}

			// Add user variables too.
			foreach (var userVar in userVariables)
				properties.Add(userVar.CreateViewModel());

			// When a property changes, redraw the viewports.
			foreach (var property in properties)
				property.PropertyChanged += (o, e) => workspace.RedrawViewports();
		}

		// Set the master per-frame callback that lets the script control rendering.
		public void SetFrameCallback(FrameCallback callback)
		{
			frameCallback = callback;
		}

		// IScriptRenderInterface implementation.

		public object LoadShader(string filename, string entryPoint, string profile)
		{
			var path = FindShader(filename);
			if (!File.Exists(path))
				throw new ScriptException("Shader file " + filename + " not found in project.");

			var shader = shaderCache.GetShader(path, entryPoint, profile);
			shaders.Add(shader);
			return new ShaderHandle(shaders.Count - 1);
		}

		// Lookup a shader filename in the project to retrieve the full path.
		private string FindShader(string name)
		{
			var shaderFileItem = workspace.Project.AllItems.FirstOrDefault(item => item.Name == name);
			if (shaderFileItem == null)
			{
				throw new ScriptException("Could not find shader file: " + name);
			}

			return shaderFileItem.AbsolutePath;
		}

		public object CreateRenderTarget()
		{
			renderTargets.Add(new RenderTargetDescriptor(new Rational(1, 1), new Rational(1, 1), true));
			return new RenderTargetHandle(renderTargets.Count - 1);
		}

		public void BindShaderVariable(dynamic shaderHandle, string varName, ShaderVariableBindSource source)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			var variable = shaders[shaderHandle.index].FindVariable(varName);
			if (variable != null)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				variable.Bind = new SimpleShaderVariableBind(variable, source);
			}
		}

		public void BindShaderVariableToMaterial(dynamic shaderHandle, string varName, string paramName)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			var variable = shaders[shaderHandle.index].FindVariable(varName);
			if (variable != null)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				variable.Bind = new MaterialShaderVariableBind(variable, paramName);
			}
		}

		public void SetShaderVariable(dynamic shaderHandle, string varName, dynamic value)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			var variable = shaders[shaderHandle.index].FindVariable(varName);
			if (variable != null)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				// Bind the variable's value to the script value.
				variable.Bind = new ScriptShaderVariableBind(variable, value, ScriptHelper);
			}
		}

		public void ShaderVariableIsScriptOverride(dynamic shaderHandle, string varName)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			IShaderVariable variable = shaders[shaderHandle.index].FindVariable(varName);
			if (variable != null)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				// Bind the variable's value to the script value.
				variable.Bind = new ScriptOverrideShaderVariableBind(variable);
			}
		}

		public void BindShaderResourceToMaterial(dynamic shaderHandle, string varName, string paramName)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			IShaderResourceVariable variable = shaders[shaderHandle.index].FindResourceVariable(varName);
			if (variable != null)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				variable.Bind = new MaterialShaderResourceVariableBind(paramName);
			}
		}

		public void SetShaderResourceVariable(dynamic shaderHandle, string varName, object value)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			IShaderResourceVariable variable = shaders[shaderHandle.index].FindResourceVariable(varName);
			if (variable != null)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				if (value is RenderTargetHandle)
				{
					// Bind the variable to a render target's SRV.
					int rtIndex = ((RenderTargetHandle)value).index;
					System.Diagnostics.Debug.Assert(rtIndex >= 0 && rtIndex < renderTargets.Count);

					variable.Bind = new RenderTargetShaderResourceVariableBind(renderTargets[rtIndex]);
				}
				else if (value is DepthBufferHandle)
				{
					var dbHandle = (DepthBufferHandle)value;
					if (dbHandle.Equals(DepthBufferHandle.Default))
					{
						// Bind to the default depth buffer.
						variable.Bind = new DefaultDepthBufferShaderResourceVariableBind();
					}
				}
				else
				{
					throw new ScriptException("Invalid parameter for shader resource variable value.");
				}
			}
		}

		// Add a user-exposed variable.
		public dynamic AddUserVar(string name, UserVariableType type, dynamic defaultValue)
		{
			var userVar = UserVariable.Create(name, type, defaultValue);
			userVariables.Add(userVar);
			return userVar.GetFunction();
		}

		public void Dispose()
		{
			RenderUtils.DisposeList(disposables);

			sphereMesh.Dispose();
			fullscreenQuad.Dispose();

			inputLayoutCache.Dispose();
			shaderCache.Dispose();
			shaders.Clear();

			RenderUtils.DisposeList(renderTargets);

			// HACK
			workspace.RenderScene.Dispose();

			device = null;
		}

		internal void Render(DeviceContext context, RenderTargetView renderTarget, ViewInfo viewInfo)
		{
			// Bail if there was a problem with the scripts.
			if (bScriptError)
				return;

			// Don't redraw viewports because of internal shader variable changes (e.g. bindings).
			bool bPrevIgnoreRedrawRequests = workspace.IgnoreRedrawRequests;
			workspace.IgnoreRedrawRequests = true;

			// Create render targets if necessary.
			UpdateRenderTargets(viewInfo.ViewportWidth, viewInfo.ViewportHeight);

			try
			{
				// Always clear the back buffer to black to avoid the script having to do so for trivial stuff.
				context.ClearRenderTargetView(renderTarget, new Color4(0));

				var renderContext = new ScriptRenderContext(
					context,
					viewInfo,
					workspace.RenderScene,
					shaders,
					(from desc in renderTargets select desc.renderTarget).ToArray(),
					inputLayoutCache,
					sphereMesh,
					fullscreenQuad,
					ScriptHelper,
					basicShaders);

				// Let the script do its thing.
				if (frameCallback != null)
					frameCallback(renderContext);
			}
			catch (Exception ex)
			{
				ScriptHelper.LogScriptError(ex);

				// Remember that the script fails so we don't just fail over and over.
				bScriptError = true;
			}

			workspace.IgnoreRedrawRequests = bPrevIgnoreRedrawRequests;
		}

		private void UpdateRenderTargets(int viewportWidth, int viewportHeight)
		{
			foreach (var desc in renderTargets)
			{
				int width = desc.GetWidth(viewportWidth);
				int height = desc.GetHeight(viewportHeight);

				// If there's no resource, or it's the wrong size, create a new one.
				if (desc.renderTarget == null || desc.renderTarget.Width != width || desc.renderTarget.Width != width)
				{
					desc.renderTarget = new RenderTarget(device, width, height);
				}
			}
		}

		private bool IsValidShader(dynamic handle)
		{
			return handle is ShaderHandle && handle.index >= 0 && handle.index < shaders.Count;
		}

		// Wrapper class that gets given to the script, acting as a firewall to prevent it from accessing this class directly.s
		internal ScriptRenderInterface ScriptInterface { get; private set; }

		private ObservableCollection<PropertyViewModel> properties = new ObservableCollection<PropertyViewModel>();
		public IEnumerable<PropertyViewModel> Properties { get { return properties; } }


		private Device device;

		// Resource arrays.
		private List<Shader> shaders = new List<Shader>();

		// List of render targets and their descritors.
		private List<RenderTargetDescriptor> renderTargets = new List<RenderTargetDescriptor>();

		// Master callback that we call each frame.
		private FrameCallback frameCallback;

		private InputLayoutCache inputLayoutCache = new InputLayoutCache();

		// Mesh to use for the DrawSphere command.
		private Mesh sphereMesh;
		private FullscreenQuad fullscreenQuad;

		// Pointer back to the workspace. Needed so we can access the project to get shaders from.
		private WorkspaceViewModel workspace;

		// Cache for compiled shaders.
		private ShaderCache shaderCache;

		// If true, previous rendering failed with a script problem, so we don't keep re-running until the script is fixed & re-run.
		private bool bScriptError = false;

		internal ScriptHelper ScriptHelper { get; set; }

		// User properties.
		private List<UserVariable> userVariables = new List<UserVariable>();

		// Basic shader types.
		private BasicShaders basicShaders;

		// List of things to dispose.
		private List<IDisposable> disposables = new List<IDisposable>();
	}
}
