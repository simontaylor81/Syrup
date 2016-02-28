using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

using SharpDX.Direct3D;
using SRPCommon.Interfaces;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using SRPCommon.Util;
using SRPRendering.Resources;
using SRPScripting;

namespace SRPRendering
{
	// Class that takes commands from the script and controls the rendering.
	class ScriptRenderControl : IDisposable, IRenderInterface
	{
		public ScriptRenderControl(IWorkspace workspace, RenderDevice device)
		{
			_workspace = workspace;
			_device = device;

			_mipGenerator = new MipGenerator(device, workspace);
		}

		public void Reset()
		{
			frameCallback = null;

			// Clear shaders array. Don't need to dispose as they're held by the cache.
			shaders.Clear();
			_userVariables.Clear();

			// Clear render target descriptors and dispose the actual render targets.
			DisposableUtil.DisposeList(renderTargets);
			DisposableUtil.DisposeList(textures);

			// Dispose resources registered for cleanup.
			DisposableUtil.DisposeList(_resources);
		}

		// Get the list of properties for a script run. Call after script execution.
		public IEnumerable<IUserProperty> GetProperties()
		{
			// Group variables by name so we don't create duplicate entries with the same name.
			// Don't add bound variables, not event as read-only
			// (as they're too slow to update every time we render the frame).
			var variablesByName = from shader in shaders
									from variable in shader.Variables
									where variable.Bind == null
									group variable by variable.Name;

			// Add shader variable property to the list for each unique name.
			var result = variablesByName
				.Select(variableGroup => ShaderVariable.CreateUserProperty(variableGroup));

			// Add user variables too.
			result = result.Concat(_userVariables);

			// Use ToList to reify the enumerable, forcing a single enumeration and any exceptions to be thrown.
			return result.ToList();
		}

		// IScriptRenderInterface implementation.

		// Set the master per-frame callback that lets the script control rendering.
		public void SetFrameCallback(FrameCallback callback)
		{
			frameCallback = callback;
		}

		public object CompileShader(
			string filename, string entryPoint, string profile, IDictionary<string, object> defines = null)
		{
			var path = FindShader(filename);
			if (!File.Exists(path))
				throw new ScriptException("Shader file " + filename + " not found in project.");

			return AddShader(_device.GlobalResources.ShaderCache.GetShader(
				path, entryPoint, profile, FindShader, ConvertDefines(defines)));
		}

		// Compile a shader from an in-memory string.
		// All includes still must come from the file system.
		public object CompileShaderFromString(string source, string entryPoint, string profile,
			IDictionary<string, object> defines = null)
		{
			// Don't cache shader from string.
			// TODO: Would be nice if we could if people are going to use them in scripts.
			return AddShader(Shader.CompileFromString(
				_device.Device, source, entryPoint, profile, FindShader, ConvertDefines(defines)));
		}

		private object AddShader(IShader shader)
		{
			shaders.Add(shader);

			// Set up auto variable binds for this shader.
			BindAutoShaderVariables(shader);

			return new ShaderHandle(shaders.Count - 1);
		}

		private ShaderMacro[] ConvertDefines(IDictionary<string, object> defines) =>
			defines
				.EmptyIfNull()
				.Select(define => new ShaderMacro(define.Key, define.Value.ToString()))
				.ToArray();

		// Lookup a shader filename in the project to retrieve the full path.
		private string FindShader(string name)
		{
			var path = _workspace.FindProjectFile(name);
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

		public void BindShaderResourceToMaterial(object handleOrHandles, string varName, string paramName, object fallback = null)
		{
			var variables = GetShaders(handleOrHandles)
				.Select(shader => shader.FindResourceVariable(varName))
				.Where(variable => variable != null);

			Texture fallbackTexture = _device.GlobalResources.ErrorTexture;

			// Ugh, Castle DynamicProxy doesn't pass through the null default value, so detect it.
			if (fallback == System.Reflection.Missing.Value)
			{
				fallback = null;
			}

			if (fallback != null)
			{
				fallbackTexture = GetTexture(fallback);
				if (fallbackTexture == null)
				{
					throw new ScriptException($"Invalid fallback texture binding {varName}");
				}
			}

			foreach (var variable in variables)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				variable.Bind = new MaterialShaderResourceVariableBind(paramName, fallbackTexture);
			}
		}

		public void SetShaderResourceVariable(object handleOrHandles, string varName, object value)
		{
			var variables = GetShaders(handleOrHandles)
				.Select(shader => shader.FindResourceVariable(varName))
				.Where(variable => variable != null);

			var texture = GetTexture(value);
			var buffer = value as BufferHandle;
			var renderTargetHandle = value as RenderTargetHandle;

			foreach (var variable in variables)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + varName);
				}

				if (texture != null)
				{
					variable.Bind = new TextureShaderResourceVariableBind(texture);
				}
				else if (buffer != null)
				{
					variable.Bind = new BufferShaderResourceVariableBind(buffer.Buffer);
				}
				else if (renderTargetHandle != null)
				{
					// Bind the variable to a render target's SRV.
					int rtIndex = renderTargetHandle.index;
					System.Diagnostics.Debug.Assert(rtIndex >= 0 && rtIndex < renderTargets.Count);

					variable.Bind = new RenderTargetShaderResourceVariableBind(renderTargets[rtIndex]);
				}
				else if (value == DepthBufferHandle.Default)
				{
					// Bind to the default depth buffer.
					variable.Bind = new DefaultDepthBufferShaderResourceVariableBind();
				}
				else
				{
					throw new ScriptException("Invalid parameter for shader resource variable value.");
				}
			}
		}

		public void SetShaderUavVariable(object handleOrHandles, string varName, IBuffer value)
		{
			var variables = GetShaders(handleOrHandles)
				.Select(shader => shader.FindUavVariable(varName))
				.Where(variable => variable != null);

			var buffer = value as BufferHandle;
			if (buffer == null)
			{
				throw new ScriptException("Invalid buffer for UAV");
			}

			foreach (var variable in variables)
			{
				if (variable.UAV != null)
				{
					throw new ScriptException("Attempting to set an already set shader variable: " + varName);
				}

				variable.UAV = buffer.Buffer.UAV;
			}
		}

		public void SetShaderSamplerState(object handleOrHandles, string samplerName, SamplerState state)
		{
			var variables = GetShaders(handleOrHandles)
				.Select(shader => shader.FindSamplerVariable(samplerName))
				.Where(variable => variable != null);

			foreach (var variable in variables)
			{
				if (variable.Bind != null)
				{
					throw new ScriptException("Attempting to bind already bound shader sampler: " + samplerName);
				}
				variable.Bind = new ShaderSamplerVariableBindDirect(state);
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

		public dynamic AddUserVar_Choice(string name, IEnumerable<object> choices, object defaultValue)
			=> AddUserVar(UserVariable.CreateChoice(name, choices, defaultValue));

		// Add a single-component user variable.
		private dynamic AddScalarUserVar<T>(string name, T defaultValue)
			=> AddUserVar(UserVariable.CreateScalar(name, defaultValue));

		// Add a vector user variable.
		private dynamic AddVectorUserVar<T>(int numComponents, string name, object defaultValue)
			=> AddUserVar(UserVariable.CreateVector<T>(numComponents, name, defaultValue));

		// Add a user variable.
		private dynamic AddUserVar<T>(UserVariable<T> userVar)
		{
			_userVariables.Add(userVar);
			return userVar.GetFunction();
		}
		#endregion

		// Create a render target of dimensions equal to the viewport.
		public object CreateRenderTarget()
		{
			renderTargets.Add(new RenderTargetDescriptor(new SharpDX.DXGI.Rational(1, 1), new SharpDX.DXGI.Rational(1, 1), true));
			return new RenderTargetHandle(renderTargets.Count - 1);
		}

		// Create a 2D texture of the given size and format, and fill it with the given data.
		public object CreateTexture2D(int width, int height, Format format, dynamic contents, bool generateMips = false)
		{
			textures.Add(Texture.CreateFromScript(_device.Device, width, height, format, contents, generateMips));
			return new TextureHandle(textures.Count - 1);
		}

		// Load a texture from disk.
		public object LoadTexture(string path, object generateMips = null)
		{
			var absPath = _workspace.GetAbsolutePath(path);

			// Ugh, Castle DynamicProxy doesn't pass through the null default value, so detect it.
			if (generateMips == System.Reflection.Missing.Value)
			{
				generateMips = null;
			}

			MipGenerationMode mipGenerationMode = MipGenerationMode.None;
			if (generateMips == null || generateMips.Equals(true))
			{
				mipGenerationMode = MipGenerationMode.Full;
			}
			else if (generateMips is string)
			{
				mipGenerationMode = MipGenerationMode.CreateOnly;
			}

			Texture texture;
			try
			{
				texture = Texture.LoadFromFile(_device.Device, absPath, mipGenerationMode);
			}
			catch (FileNotFoundException ex)
			{
				throw new ScriptException("Could not file texture file: " + absPath, ex);
			}
			catch (Exception ex)
			{
				throw new ScriptException("Error loading texture file: " + absPath, ex);
			}

			// We want mip generation errors to be reported directly, so this is
			// outside the above try-catch.
			if (mipGenerationMode == MipGenerationMode.CreateOnly)
			{
				// Generate custom mips.
				_mipGenerator.Generate(texture, generateMips as string);
			}

			textures.Add(texture);
			return new TextureHandle(textures.Count - 1);
		}

		// Create a buffer of the given size and format, and fill it with the given data.
		public IBuffer CreateBuffer(int sizeInBytes, Format format, dynamic contents, bool uav = false) =>
			AddResource(BufferHandle.CreateDynamic(_device, sizeInBytes, uav, format, contents));

		// Create a structured buffer.
		public IBuffer CreateStructuredBuffer<T>(IEnumerable<T> contents, bool uav = false) where T : struct =>
			AddResource(BufferHandle.CreateStructured(_device, uav, contents));

		public void Dispose()
		{
			Reset();

			shaders.Clear();
			DisposableUtil.DisposeList(renderTargets);

			_device = null;
		}

		public void Render(SharpDX.Direct3D11.DeviceContext deviceContext, ViewInfo viewInfo, RenderScene renderScene)
		{
			// Create render targets if necessary.
			UpdateRenderTargets(viewInfo.ViewportWidth, viewInfo.ViewportHeight);

			// Let the script do its thing.
			if (frameCallback != null)
			{
				var renderContext = new ScriptRenderContext(
					deviceContext,
					viewInfo,
					renderScene,
					shaders,
					(from desc in renderTargets select desc.renderTarget).ToArray(),
					_device.GlobalResources);

				frameCallback(renderContext);
			}
		}

		private void UpdateRenderTargets(int viewportWidth, int viewportHeight)
		{
			foreach (var desc in renderTargets)
			{
				int width = desc.GetWidth(viewportWidth);
				int height = desc.GetHeight(viewportHeight);

				// If there's no resource, or it's the wrong size, create a new one.
				if (desc.renderTarget == null || desc.renderTarget.Width != width || desc.renderTarget.Height != height)
				{
					// Don't forget to release the old one.
					desc.renderTarget?.Dispose();

					// TODO: Custom format
					desc.renderTarget = new RenderTarget(_device.Device, width, height, SharpDX.DXGI.Format.R8G8B8A8_UNorm);
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

		// Get the texture to use for a shader resource variable.
		private Texture GetTexture(object handle)
		{
			if (handle == BlackTexture)
			{
				return _device.GlobalResources.BlackTexture;
			}
			else if (handle == WhiteTexture)
			{
				return _device.GlobalResources.WhiteTexture;
			}
			else if (handle == DefaultNormalTexture)
			{
				return _device.GlobalResources.DefaultNormalTexture;
			}
			else if (handle is TextureHandle)
			{
				// Bind the variable to a texture's SRV.
				int texIndex = ((TextureHandle)handle).index;
				System.Diagnostics.Debug.Assert(texIndex >= 0 && texIndex < textures.Count);

				return textures[texIndex];
			}

			return null;
		}

		// Register a resource for later disposal, returning it for easy chaining.
		private T AddResource<T>(T resource) where T : IDisposable
		{
			_resources.Add(resource);
			return resource;
		}

		public dynamic GetScene() => Scene;

		public object DepthBuffer => DepthBufferHandle.Default;
		public object NoDepthBuffer => DepthBufferHandle.NoDepthBuffer;

		// These don't need to be anything, we're just going to use them with reference equality checks.
		public object BlackTexture { get; } = new object();
		public object WhiteTexture { get; } = new object();
		public object DefaultNormalTexture { get; } = new object();


		public Scene Scene { get; set; }

		private RenderDevice _device;

		// Resource arrays.
		private List<IShader> shaders = new List<IShader>();

		// Script-generated resources.
		private List<Texture> textures = new List<Texture>();

		// List of resource to be disposed of when reseting or disposing.
		private List<IDisposable> _resources = new List<IDisposable>();

		// List of render targets and their descritors.
		private List<RenderTargetDescriptor> renderTargets = new List<RenderTargetDescriptor>();

		// Master callback that we call each frame.
		private FrameCallback frameCallback;

		// Pointer back to the workspace. Needed so we can access the project to get shaders from.
		private IWorkspace _workspace;

		// User variables.
		private List<IUserProperty> _userVariables = new List<IUserProperty>();

		private readonly MipGenerator _mipGenerator;
	}
}
