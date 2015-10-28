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
using Castle.DynamicProxy;

namespace SRPRendering
{
	// Class that takes commands from the script and controls the rendering.
	public class ScriptRenderControl : IDisposable, IPropertySource, IRenderInterface
	{
		public ScriptRenderControl(IWorkspace workspace, Device device, Scripting scripting)
		{
			this.workspace = workspace;
			this.device = device;

			// Generate wrapper proxy using Castle Dynamic Proxy to avoid direct script access to our internals.
			ScriptInterface = new ProxyGenerator().CreateInterfaceProxyWithTarget<IRenderInterface>(this);

			// Initialise basic resources.
			_globalResources = new GlobalResources(device);
			disposables.Add(_globalResources);

			// Reset before script execution.
			disposables.Add(scripting.PreExecute.Subscribe(PreExecuteScript));
			disposables.Add(scripting.ExecutionComplete.Subscribe(ExecutionComplete));

			overlayRenderer = new OverlayRenderer(_globalResources);
		}

		private void PreExecuteScript(Script script)
		{
			Reset();
			_currentScript = script;
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
			bScriptRenderError = false;

			var script = _currentScript;
			_currentScript = null;

			try
			{
				// Group variables by name so we don't create duplicate entries with the same name.
				// Don't add bound variables, not event as read-only
				// (as they're too slow to update every time we render the frame).
				var variablesByName = from shader in shaders
									  from variable in shader.Variables
									  where variable.Bind == null
									  group variable by variable.Name;

				// Add shader variable property to the list for each unique name.
				foreach (var variableGroup in variablesByName)
				{
					properties.Add(ShaderVariable.CreateUserProperty(variableGroup));
				}
			}
			catch (ScriptException ex)
			{
				ScriptHelper.Instance.LogScriptError(ex);
				bScriptExecutionError = true;
				return;
			}

			// Add user variables too.
			foreach (var userVar in userVariables)
			{
				properties.Add(userVar);
			}

			foreach (var property in properties)
			{
				IUserProperty prevProperty;
				if (script.UserProperties.TryGetValue(property.Name, out prevProperty))
				{
					// Copy value from existing property.
					property.TryCopyFrom(prevProperty);
				}

				// Save this property for next time.
				script.UserProperties[property.Name] = property;
			}

			// When a property changes, redraw the viewports.
			foreach (var property in properties)
			{
				disposables.Add(property.Subscribe(_ => FireRedrawRequired()));
			}

			bScriptExecutionError = !bSuccess;
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

		public object CompileShader(
			string filename, string entryPoint, string profile, IDictionary<string, object> defines)
		{
			var path = FindShader(filename);
			if (!File.Exists(path))
				throw new ScriptException("Shader file " + filename + " not found in project.");

			var macros = defines
				.EmptyIfNull()
				.Select(define => new ShaderMacro(define.Key, define.Value.ToString()))
				.ToArray();

			var shader = _globalResources.ShaderCache.GetShader(path, entryPoint, profile, FindShader, macros);
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

		public void BindShaderVariable(object handleOrHandles, string varName, ShaderVariableBindSource source)
		{
			var shaders = GetShaders(handleOrHandles);
			var variables = shaders.Select(shader => shader.FindVariable(varName));
			SetShaderBind(variables, variable => new SimpleShaderVariableBind(variable, source));
		}

		public void BindShaderVariableToMaterial(object handleOrHandles, string varName, string paramName)
		{
			var shaders = GetShaders(handleOrHandles);
			var variables = shaders.Select(shader => shader.FindVariable(varName));
			SetShaderBind(variables, variable => new MaterialShaderVariableBind(variable, paramName));
		}

		public void SetShaderVariable(object handleOrHandles, string varName, dynamic value)
		{
			var shaders = GetShaders(handleOrHandles);
			var variables = shaders.Select(shader => shader.FindVariable(varName));
			SetShaderBind(variables, variable => new ScriptShaderVariableBind(variable, value));
		}

		public void ShaderVariableIsScriptOverride(object handleOrHandles, string varName)
		{
			var shaders = GetShaders(handleOrHandles);
			var variables = shaders.Select(shader => shader.FindVariable(varName));
			SetShaderBind(variables, variable => new ScriptOverrideShaderVariableBind(variable));
		}

		// Simple helper to avoid duplication.
		// If the passed in variable is valid, and it is not already bound, sets its
		// bind to the result of the given function.
		private void SetShaderBind(IEnumerable<IShaderVariable> variables,
			Func<IShaderVariable, IShaderVariableBind> createBind)
		{
			foreach (var variable in variables)
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
					variable.Bind = createBind(variable);
					variable.IsAutoBound = false;
				}
			}
		}

		public void BindShaderResourceToMaterial(object handleOrHandles, string varName, string paramName)
		{
			var shaders = GetShaders(handleOrHandles);
			var variables = shaders
				.Select(shader => shader.FindResourceVariable(varName))
				.Where(shader => shader != null);

			foreach (var variable in variables)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				variable.Bind = new MaterialShaderResourceVariableBind(paramName);
			}
		}

		public void SetShaderResourceVariable(object handleOrHandles, string varName, object value)
		{
			var shaders = GetShaders(handleOrHandles);
			var variables = shaders
				.Select(shader => shader.FindResourceVariable(varName))
				.Where(shader => shader != null);

			foreach (var variable in variables)
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

		#region User Variables
		public dynamic AddUserVar_Float(string name, float defaultValue) => AddScalarUserVar<float>(name, defaultValue);
		public dynamic AddUserVar_Float2(string name, object defaultValue) => AddVectorUserVar<float>(2, name, defaultValue);
		public dynamic AddUserVar_Float3(string name, object defaultValue) => AddVectorUserVar<float>(3, name, defaultValue);
		public dynamic AddUserVar_Float4(string name, object defaultValue) => AddVectorUserVar<float>(4, name, defaultValue);
		public dynamic AddUserVar_Int(string name, int defaultValue) => AddScalarUserVar<int>(name, defaultValue);
		public dynamic AddUserVar_Int2(string name, object defaultValue) => AddVectorUserVar<int>(2, name, defaultValue);
		public dynamic AddUserVar_Int3(string name, object defaultValue) => AddVectorUserVar<int>(3, name, defaultValue);
		public dynamic AddUserVar_Int4(string name, object defaultValue) => AddVectorUserVar<int>(4, name, defaultValue);
		public dynamic AddUserVar_Bool(string name, bool defaultValue) => AddScalarUserVar<bool>(name, defaultValue);
		public dynamic AddUserVar_String(string name, string defaultValue) => AddScalarUserVar<string>(name, defaultValue);

		// Add a single-component user variable.
		private dynamic AddScalarUserVar<T>(string name, T defaultValue)
			=> AddUserVar(UserVariable.CreateScalar(name, defaultValue));

		// Add a vector user variable.
		private dynamic AddVectorUserVar<T>(int numComponents, string name, object defaultValue)
			=> AddUserVar(UserVariable.CreateVector<T>(numComponents, name, defaultValue));

		// Add a user variable.
		private dynamic AddUserVar(UserVariable userVar)
		{
			userVariables.Add(userVar);
			return userVar.GetFunction();
		}
		#endregion

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

		private bool IsValidShaderHandle(ShaderHandle handle)
			=> handle != null && handle.index >= 0 && handle.index < shaders.Count;

		// Given a shader handle or list of handles, get a list of shaders they correspond to.
		private IEnumerable<IShader> GetShaders(object handleOrHandles)
		{
			var handle = handleOrHandles as ShaderHandle;
			var handleList = handleOrHandles as IEnumerable<object>;

			IEnumerable<ShaderHandle> handles = null;
			if (handle != null)
			{
				handles = EnumerableEx.Return(handle);
			}
			else if (handleList != null)
			{
				handles = handleList.Select(h => h as ShaderHandle);
			}

			if (handles == null || handles.Any(h => !IsValidShaderHandle(h)))
			{
				throw new ScriptException("Invalid shader.");
			}

			return handles.Select(h => shaders[h.index]);
		}

		public dynamic GetScene() => Scene;

		public object DepthBuffer => DepthBufferHandle.Default;
		public object NoDepthBuffer => DepthBufferHandle.NoDepthBuffer;

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

		// Script currently being executed.
		private Script _currentScript;

		// List of things to dispose.
		private CompositeDisposable disposables = new CompositeDisposable();
	}
}
