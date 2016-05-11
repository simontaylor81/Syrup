using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using SharpDX.Direct3D;
using SRPCommon.Interfaces;
using SRPCommon.Logging;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using SRPCommon.Util;
using SRPRendering.Resources;
using SRPRendering.Shaders;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering
{
	// Class that takes commands from the script and controls the rendering.
	class ScriptRenderControl : IDisposable, IRenderInterface
	{
		public ScriptRenderControl(
			IWorkspace workspace,
			RenderDevice device,
			ILoggerFactory loggerFactory,
			IDictionary<string, IUserProperty> existingUserProperties)
		{
			_workspace = workspace;
			_device = device;
			_existingUserProperties = existingUserProperties;

			_logLogger = loggerFactory.CreateLogger("Log");
			_scriptLogger = loggerFactory.CreateLogger("Script");
			_shaderCompileLogger = loggerFactory.CreateLogger("ShaderCompile");

			_mipGenerator = new MipGenerator(device, workspace, _scriptLogger);

			BlackTexture = new DefaultTextureHandle(device.GlobalResources.BlackTexture);
			WhiteTexture = new DefaultTextureHandle(device.GlobalResources.WhiteTexture);
			DefaultNormalTexture = new DefaultTextureHandle(device.GlobalResources.DefaultNormalTexture);
		}

		// Get the list of properties for a script run. Call after script execution.
		public IEnumerable<IUserProperty> GetProperties()
		{
			// Group variables by name so we don't create duplicate entries with the same name.
			// Don't add bound variables, not even as read-only
			// (as they're too slow to update every time we render the frame).
			var variablesByName = from shader in shaders
								  from variable in shader.ConstantVariables
								  where variable.Binding == null
								  group variable by variable.Name;

			// Add shader variable property to the list for each unique name.
			var shaderProperties = variablesByName
				.Select(variableGroup =>
				{
					var property = ShaderUserProperties.Create(variableGroup);

					// Copy values from previous runs, if possible.
					IUserProperty prevProperty;
					if (_existingUserProperties.TryGetValue(property.Name, out prevProperty))
					{
						// Copy value from existing property.
						property.TryCopyFrom(prevProperty);
					}

					return property;
				});

			// Add user variables too.
			var result = shaderProperties.Concat(_userVariables);

			// Use ToList to reify the enumerable, forcing a single enumeration and any exceptions to be thrown.
			return result.ToList();
		}

		// IScriptRenderInterface implementation.

		// Set the master per-frame callback that lets the script control rendering.
		public void SetFrameCallback(FrameCallback callback)
		{
			frameCallback = callback;
		}

		public IShader CompileShader(
			string filename, string entryPoint, string profile, IDictionary<string, object> defines = null)
		{
			var path = FindShader(filename);
			if (!File.Exists(path))
				throw new ScriptException("Shader file " + filename + " not found in project.");

			// Don't actually compile anything here -- defer so we can async/multithread it.
			return _shaderHandles.AddAndReturn(new ShaderHandle(path, entryPoint, profile, FindShader, ConvertDefines(defines)));
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

		#region User Variables
		public Func<float> AddUserVar_Float(string name, float defaultValue) => AddScalarUserVar<float>(name, defaultValue);
		public Func<float[]> AddUserVar_Float2(string name, object defaultValue) => AddVectorUserVar<float>(2, name, defaultValue);
		public Func<float[]> AddUserVar_Float3(string name, object defaultValue) => AddVectorUserVar<float>(3, name, defaultValue);
		public Func<float[]> AddUserVar_Float4(string name, object defaultValue) => AddVectorUserVar<float>(4, name, defaultValue);
		public Func<int> AddUserVar_Int(string name, int defaultValue) => AddScalarUserVar<int>(name, defaultValue);
		public Func<int[]> AddUserVar_Int2(string name, object defaultValue) => AddVectorUserVar<int>(2, name, defaultValue);
		public Func<int[]> AddUserVar_Int3(string name, object defaultValue) => AddVectorUserVar<int>(3, name, defaultValue);
		public Func<int[]> AddUserVar_Int4(string name, object defaultValue) => AddVectorUserVar<int>(4, name, defaultValue);
		public Func<bool> AddUserVar_Bool(string name, bool defaultValue) => AddScalarUserVar<bool>(name, defaultValue);
		public Func<string> AddUserVar_String(string name, string defaultValue) => AddScalarUserVar<string>(name, defaultValue);

		public Func<object> AddUserVar_Choice(string name, IEnumerable<object> choices, object defaultValue)
			=> AddUserVar(UserVariable.CreateChoice(name, choices, defaultValue));

		// Add a single-component user variable.
		private dynamic AddScalarUserVar<T>(string name, T defaultValue)
			=> AddUserVar(UserVariable.CreateScalar(name, defaultValue));

		// Add a vector user variable.
		private dynamic AddVectorUserVar<T>(int numComponents, string name, object defaultValue)
			=> AddUserVar(UserVariable.CreateVector<T>(numComponents, name, defaultValue));

		// Add a user variable.
		private Func<T> AddUserVar<T>(UserVariable<T> userVar)
		{
			// Copy value from previous runs, if possible.
			IUserProperty prevProperty;
			if (_existingUserProperties.TryGetValue(userVar.Name, out prevProperty))
			{
				// Copy value from existing property.
				userVar.TryCopyFrom(prevProperty);
			}

			_userVariables.Add(userVar);

			// Wrap function to track access.
			var function = userVar.GetFunction();
			return () =>
			{
				// If the variable is accessed outside of the render callback, then we must re-execute
				// the script to have changes take effect, so mark the variable as such.
				if (!_bRendering)
				{
					userVar.RequiresReExecute = true;
				}
				return function();
			};
		}

		#endregion

		// Create a render target of dimensions equal to the viewport.
		public IRenderTarget CreateRenderTarget()
		{
			return _renderTargets.AddAndReturn(new RenderTargetHandle(
				new SharpDX.DXGI.Rational(1, 1), new SharpDX.DXGI.Rational(1, 1), true));
		}

		// Create a 2D texture of the given size and format, and fill it with the given data.
		public ITexture2D CreateTexture2D<T>(int width, int height, Format format, IEnumerable<T> contents)
		{
			return _deferredResources.AddAndReturn(new TextureHandleEnumerable<T>(width, height, format, contents));
		}

		// Create a 2D texture of the given size and format, and fill it with data from the given callback.
		public ITexture2D CreateTexture2D(int width, int height, Format format, Func<int, int, object> contentCallback)
		{
			var contents = EnumerableUtil.Range2D(width, height, contentCallback);
			return _deferredResources.AddAndReturn(new TextureHandleEnumerable<object>(width, height, format, contents));
		}

		// Load a texture from disk.
		public ITexture2D LoadTexture(string path)
		{
			var absPath = _workspace.GetAbsolutePath(path);
			return _deferredResources.AddAndReturn(new TextureHandleFile(absPath));
		}

		// Create a structured buffer containing the given contents exactly as it is.
		public IBuffer CreateBuffer<T>(IEnumerable<T> contents) where T : struct =>
			_deferredResources.AddAndReturn(new BufferHandleStructured<T>(contents));

		// Create an buffer of the given size and format, optionally with initial data that is converted to the correct format.
		public IBuffer CreateTypedBuffer<T>(int numElements, Format format, IEnumerable<T> contents) =>
			_deferredResources.AddAndReturn(new BufferHandleFormatted<T>(numElements, format, contents));

		// Create an uninitialised buffer of the given size and format, to be written to by the GPU.
		public IBuffer CreateUninitialisedBuffer(int sizeInBytes, int stride) =>
			_deferredResources.AddAndReturn(new BufferHandleUnitialised(sizeInBytes, stride));

		public void Dispose()
		{
			frameCallback = null;

			// Clear shaders array. Don't need to dispose as they're held by the cache.
			shaders.Clear();
			_shaderHandles.Clear();
			_userVariables.Clear();

			_deferredResources.Clear();

			// Clear render target handles and dispose the actual render targets.
			DisposableUtil.DisposeList(_renderTargets);

			// Dispose resources registered for cleanup.
			DisposableUtil.DisposeList(_resources);

			_device = null;
		}

		// Called after script has finished executing (successfull).
		public async Task ScriptExecutionComplete(IProgress progress)
		{
			progress.Update("Compiling shdaers...");
			await CompileShadersAsync();

			progress.Update("Loading textures...");
			await CreateResourcesAsync();
		}

		// Compile all shaders once we're done executing the script.
		private async Task CompileShadersAsync()
		{
			// Run on background thread so UI remains responsive.
			shaders = await Task.Run(() => _shaderHandles
				.AsParallel()
				.Select(GuardedCompileShader)
				.ToList());

			// Null results mean the compilation failed.
			int numFailures = shaders.Count(x => x == null);
			if (numFailures > 0)
			{
				throw new ScriptException($"{numFailures} shader{(numFailures > 1 ? "s" : "")} failed to compile. See ShaderCompile output for more info.");
			}
		}

		// Compile an individual shader, with error handling.
		private Shader GuardedCompileShader(ShaderHandle handle)
		{
			try
			{
				return handle.Compile(_device.GlobalResources.ShaderCache);
			}
			catch (ScriptException ex)
			{
				// Log errors to the ShaderCompile log window.
				_shaderCompileLogger.Log(ex.Message);

				// Don't rethrow here so we gather errors from all shaders (don't fail fast).
				return null;
			}
		}

		// Create all deferred-creation resources (texture, buffers, etc.)
		private Task CreateResourcesAsync()
		{
			// Poor mans async: run on background thread.
			return Task.Run(() =>
			{
				foreach (var handle in _deferredResources)
				{
					handle.CreateResource(_device, _logLogger, _mipGenerator);
					_resources.Add(handle.Resource);
				}
			});
		}

		public void Render(SharpDX.Direct3D11.DeviceContext deviceContext, ViewInfo viewInfo, RenderScene renderScene)
		{
			// Create render targets if necessary.
			UpdateRenderTargets(viewInfo);

			// Let the script do its thing.
			if (frameCallback != null)
			{
				var renderContext = new DeferredRenderContext(
					viewInfo,
					renderScene,
					_device.GlobalResources,
					_scriptLogger);

				try
				{
					_bRendering = true;
					frameCallback(renderContext);
				}
				finally
				{
					_bRendering = false;
				}

				renderContext.Execute(deviceContext);
			}
		}

		private void UpdateRenderTargets(ViewInfo viewInfo)
		{
			foreach (var rt in _renderTargets)
			{
				rt.UpdateSize(_device.Device, viewInfo.ViewportWidth, viewInfo.ViewportHeight);
			}
		}

		// Register a resource for later disposal, returning it for easy chaining.
		private T AddResource<T>(T resource) where T : IDisposable
		{
			_resources.Add(resource);
			return resource;
		}

		public dynamic GetScene() => Scene;

		public void Log(string line)
		{
			_scriptLogger.LogLine(line);
		}

		public IDepthBuffer DepthBuffer => DepthBufferHandle.Default;
		public IDepthBuffer NoDepthBuffer => DepthBufferHandle.Null;

		public IShaderResource BlackTexture { get; }
		public IShaderResource WhiteTexture { get; }
		public IShaderResource DefaultNormalTexture { get; }


		public Scene Scene { get; set; }

		private RenderDevice _device;

		// List of shaders. Needed to gather user properties.
		private List<Shader> shaders = new List<Shader>();
		private List<ShaderHandle> _shaderHandles = new List<ShaderHandle>();

		// Handles to textures requested by script (actual resource creation is deferred).
		private List<IDeferredResource> _deferredResources = new List<IDeferredResource>();

		// List of resource to be disposed of when reseting or disposing.
		private List<IDisposable> _resources = new List<IDisposable>();

		// List of render targets and their descritors.
		private List<RenderTargetHandle> _renderTargets = new List<RenderTargetHandle>();

		// Master callback that we call each frame.
		private FrameCallback frameCallback;

		// Pointer back to the workspace. Needed so we can access the project to get shaders from.
		private IWorkspace _workspace;

		// User variables.
		private List<IUserProperty> _userVariables = new List<IUserProperty>();

		// User properties from previous runs to seed new properties with.
		private readonly IDictionary<string, IUserProperty> _existingUserProperties;

		private readonly MipGenerator _mipGenerator;

		// Are we currently rendering?
		private bool _bRendering = false;

		private readonly ILogger _logLogger;
		private readonly ILogger _scriptLogger;
		private readonly ILogger _shaderCompileLogger;

	}
}
