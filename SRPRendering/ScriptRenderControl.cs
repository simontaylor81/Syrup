using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;

using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SRPCommon.Interfaces;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using SRPCommon.Util;
using SRPScripting;
using System.Reactive;
using System.Reactive.Subjects;

namespace SRPRendering
{
	// Class that takes commands from the script and controls the rendering.
	// Anything not part of the IRenderInterface is marked internal to avoid the script calling it.
	public class ScriptRenderControl : IDisposable, IPropertySource
	{
		public ScriptRenderControl(IWorkspace workspace, Device device, Scripting scripting)
		{
			this.workspace = workspace;
			this.device = device;

			ScriptInterface = new ScriptRenderInterface(this);

			// Initialise basic resources.
			_globalResources = new GlobalResources(device);
			disposables.Add(_globalResources);

			// Reset before script execution.
			disposables.Add(scripting.PreExecute.Subscribe(_ => Reset()));
			disposables.Add(scripting.ExecutionComplete.Subscribe(ExecutionComplete));

			overlayRenderer = new OverlayRenderer(_globalResources);
		}

		private void Reset()
		{
			frameCallback = null;

			// Clear shaders array. Don't need to dispose as they're held by the cache.
			shaders.Clear();
			properties.Clear();
			userVariables.Clear();

			// Clear render target descriptors and dispose the actual render targets.
			DisposableUtil.DisposeList(renderTargets);
			DisposableUtil.DisposeList(textures);

			// Reset output logger so warnings are written again.
			OutputLogger.Instance.ResetLogOnce();
		}

		private void ExecutionComplete(bool bSuccess)
		{
			bScriptExecutionError = !bSuccess;
			bScriptRenderError = false;

			// Add shader variables to the properties list.
			foreach (var shader in shaders)
			{
				foreach (var variable in shader.Variables)
				{
					// Don't add bound variables.
					// Don't even add them as read-only, as they're too slow to update every time we render the frame.
					if (variable.Bind == null)
					{
						properties.Add(ShaderVariable.CreateUserProperty(variable));
					}
				}
			}

			// Add user variables too.
			foreach (var userVar in userVariables)
			{
				properties.Add(userVar);
			}

			// When a property changes, redraw the viewports.
			foreach (var property in properties)
			{
				disposables.Add(property.Subscribe(_ => FireRedrawRequired()));
			}
		}

		public IObservable<Unit> RedrawRequired => _redrawRequired;
		private readonly Subject<Unit> _redrawRequired = new Subject<Unit>();
		private bool _bIgnoreRedrawRequests;

		private void FireRedrawRequired()
		{
			if (!this._bIgnoreRedrawRequests)
			{
				_redrawRequired.OnNext(Unit.Default);
			}
		}

		public Scene Scene
		{
			get { return baseScene; }
			set
			{
				if (baseScene != value)
				{
					baseScene = value;

					// Dispose of the old scene.
					DisposableUtil.SafeDispose(scene);

					// Create new one.
					scene = new RenderScene(baseScene, device, _globalResources);

					// Missing scene can cause rendering to fail -- give it another try with the new one.
					bScriptRenderError = false;
					FireRedrawRequired();
				}
			}
		}

		// Set the master per-frame callback that lets the script control rendering.
		public void SetFrameCallback(FrameCallback callback)
		{
			frameCallback = callback;
		}

		// IScriptRenderInterface implementation.

		public object CompileShader(string filename, string entryPoint, string profile)
		{
			var path = FindShader(filename);
			if (!File.Exists(path))
				throw new ScriptException("Shader file " + filename + " not found in project.");

			var shader = _globalResources.ShaderCache.GetShader(path, entryPoint, profile, FindShader);
			shaders.Add(shader);

			// Set up auto variable binds for this shader.
			BindAutoShaderVariables(shader);

			return new ShaderHandle(shaders.Count - 1);
		}

		// Lookup a shader filename in the project to retrieve the full path.
		private string FindShader(string name)
		{
			var path = workspace.FindProjectFile(name);
			if (path == null)
			{
				throw new ScriptException("Could not find shader file: " + name);
			}

			return path;
		}

		// Set up auto variable binds for a shader
		private void BindAutoShaderVariables(IShader shader)
		{
			foreach (var variable in shader.Variables)
			{
				// We auto bind variable with the same name as a bind source.
				ShaderVariableBindSource source;
				if (Enum.TryParse(variable.Name, out source))
				{
					// Nothing should be bound yet.
					System.Diagnostics.Debug.Assert(variable.Bind == null);
					variable.Bind = new SimpleShaderVariableBind(variable, source);
					variable.IsAutoBound = true;
				}
			}
		}

		public void BindShaderVariable(dynamic shaderHandle, string varName, ShaderVariableBindSource source)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			IShaderVariable variable = shaders[shaderHandle.index].FindVariable(varName);
			SetShaderBind(variable, () => new SimpleShaderVariableBind(variable, source));
		}

		public void BindShaderVariableToMaterial(dynamic shaderHandle, string varName, string paramName)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			IShaderVariable variable = shaders[shaderHandle.index].FindVariable(varName);
			SetShaderBind(variable, () => new MaterialShaderVariableBind(variable, paramName));
		}

		public void SetShaderVariable(dynamic shaderHandle, string varName, dynamic value)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			IShaderVariable variable = shaders[shaderHandle.index].FindVariable(varName);
			SetShaderBind(variable, () => new ScriptShaderVariableBind(variable, value));
		}

		public void ShaderVariableIsScriptOverride(dynamic shaderHandle, string varName)
		{
			if (!IsValidShader(shaderHandle))
				throw new ScriptException("Invalid shader.");

			IShaderVariable variable = shaders[shaderHandle.index].FindVariable(varName);
			SetShaderBind(variable, () => new ScriptOverrideShaderVariableBind(variable));
		}

		// Simple helper to avoid duplication.
		// If the passed in variable is valid, and it is not already bound, sets its
		// bind to the result of the given function.
		private void SetShaderBind(IShaderVariable variable, Func<IShaderVariableBind> createBind)
		{
			// Silently fail on null (not-found) variable, as they can be removed by optimisation.
			if (variable != null)
			{
				// Allow manual override of auto-binds
				if (variable.Bind != null && !variable.IsAutoBound)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + variable.Name);
				}

				// Bind the variable's value to the script value.
				variable.Bind = createBind();
				variable.IsAutoBound = false;
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

				if (value is TextureHandle)
				{
					// Bind the variable to a texture's SRV.
					int texIndex = ((TextureHandle)value).index;
					System.Diagnostics.Debug.Assert(texIndex >= 0 && texIndex < textures.Count);

					variable.Bind = new TextureShaderResourceVariableBind(textures[texIndex]);
				}
				else if (value is RenderTargetHandle)
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

		// Create a render target of dimensions equal to the viewport.
		public object CreateRenderTarget()
		{
			renderTargets.Add(new RenderTargetDescriptor(new Rational(1, 1), new Rational(1, 1), true));
			return new RenderTargetHandle(renderTargets.Count - 1);
		}

		// Create a 2D texture of the given size and format, and fill it with the given data.
		public object CreateTexture2D(int width, int height, Format format, dynamic contents)
		{
			textures.Add(Texture.CreateFromScript(device, width, height, format, contents));
			return new TextureHandle(textures.Count - 1);
		}

		// Load a texture from disk.
		public object LoadTexture(string path)
		{
			textures.Add(Texture.LoadFromFile(device, workspace.GetAbsolutePath(path)));
			return new TextureHandle(textures.Count - 1);
		}

		public void Dispose()
		{
			Reset();

			disposables.Dispose();

			inputLayoutCache.Dispose();
			shaders.Clear();
			DisposableUtil.SafeDispose(scene);

			DisposableUtil.DisposeList(renderTargets);

			device = null;
		}

		public void Render(DeviceContext deviceContext, ViewInfo viewInfo)
		{
			// Bail if there was a problem with the scripts.
			if (HasScriptError)
				return;

			// Don't redraw viewports because of internal shader variable changes (e.g. bindings).
			_bIgnoreRedrawRequests = true;

			// Create render targets if necessary.
			UpdateRenderTargets(viewInfo.ViewportWidth, viewInfo.ViewportHeight);

			try
			{
				// Always clear the back buffer to black to avoid the script having to do so for trivial stuff.
				deviceContext.ClearRenderTargetView(viewInfo.BackBuffer, new Color4(0));

				// Let the script do its thing.
				if (frameCallback != null)
				{
					var renderContext = new ScriptRenderContext(
						deviceContext,
						viewInfo,
						scene,
						shaders,
						(from desc in renderTargets select desc.renderTarget).ToArray(),
						_globalResources);

					frameCallback(renderContext);
				}
			}
			catch (Exception ex)
			{
				ScriptHelper.Instance.LogScriptError(ex);

				// Remember that the script fails so we don't just fail over and over.
				bScriptRenderError = true;
			}

			// Make sure we're rendering to the back buffer before rendering the overlay.
			deviceContext.OutputMerger.SetTargets(viewInfo.DepthBuffer.DSV, viewInfo.BackBuffer);
			deviceContext.Rasterizer.SetViewports(new Viewport(0.0f, 0.0f, viewInfo.ViewportWidth, viewInfo.ViewportHeight));

			// Render the overlay.
			overlayRenderer.Draw(deviceContext, scene, viewInfo);

			_bIgnoreRedrawRequests = false;
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
			=> handle is ShaderHandle && handle.index >= 0 && handle.index < shaders.Count;

		// Wrapper class that gets given to the script, acting as a firewall to prevent it from accessing this class directly.
		public IRenderInterface ScriptInterface { get; }

		private ObservableCollection<IUserProperty> properties = new ObservableCollection<IUserProperty>();
		public ObservableCollection<IUserProperty> Properties => properties;
		IEnumerable<IUserProperty> IPropertySource.Properties => properties;

		private Device device;

		// Resource arrays.
		private List<IShader> shaders = new List<IShader>();

		// Script-generated resources.
		private List<Texture> textures = new List<Texture>();

		// List of render targets and their descritors.
		private List<RenderTargetDescriptor> renderTargets = new List<RenderTargetDescriptor>();

		// Master callback that we call each frame.
		private FrameCallback frameCallback;

		private InputLayoutCache inputLayoutCache = new InputLayoutCache();

		// Miscellaneous shared resources.
		private IGlobalResources _globalResources;

		// Pointer back to the workspace. Needed so we can access the project to get shaders from.
		private IWorkspace workspace;

		private bool bScriptExecutionError = false;		// True if there was a problem executing the script
		private bool bScriptRenderError = false;        // True if there was a script error while rendering

		// If true, previous rendering failed with a script problem, so we don't keep re-running until the script is fixed & re-run.
		public bool HasScriptError => bScriptExecutionError || bScriptRenderError;

		// User variables.
		private List<UserVariable> userVariables = new List<UserVariable>();

		// Object that handles rendering the viewport overlay.
		private OverlayRenderer overlayRenderer;

		// Renderer representation of the scene we're currently rendering
		private RenderScene scene;

		// Original scene data the above was created from.
		private Scene baseScene;

		// List of things to dispose.
		private CompositeDisposable disposables = new CompositeDisposable();
	}
}
