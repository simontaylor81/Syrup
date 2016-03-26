﻿using System;
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
		public ScriptRenderControl(IWorkspace workspace, RenderDevice device, ILoggerFactory loggerFactory)
		{
			_workspace = workspace;
			_device = device;

			_logLogger = loggerFactory.CreateLogger("Log");
			_scriptLogger = loggerFactory.CreateLogger("Script");
			_shaderCompileLogger = loggerFactory.CreateLogger("ShaderCompile");

			_mipGenerator = new MipGenerator(device, workspace, _scriptLogger);
		}

		public void Reset()
		{
			frameCallback = null;

			// Clear shaders array. Don't need to dispose as they're held by the cache.
			shaders.Clear();
			_shaderHandles.Clear();
			_userVariables.Clear();

			// Clear render target handles and dispose the actual render targets.
			DisposableUtil.DisposeList(_renderTargets);

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
								  from variable in shader.ConstantVariables
								  where variable.Binding == null
								  group variable by variable.Name;

			// Add shader variable property to the list for each unique name.
			var result = variablesByName
				.Select(variableGroup => ShaderUserProperties.Create(variableGroup));

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
		private Func<T> AddUserVar<T>(UserVariable<T> userVar)
		{
			_userVariables.Add(userVar);

			// Wrap function to check safety.
			var function = userVar.GetFunction();
			return () =>
			{
				if (!_isUserVarEvalSafe)
				{
					throw new ScriptException("User variables cannot be evaluated during initial script execution");
				}
				return function();
			};
		}

		// Is it safe to read user variables?
		// Currently only true during rendering.
		// Not safe during initial execution, as it won't be
		// re-evaluated when the property is changed by the user.
		private bool _isUserVarEvalSafe = false;

		#endregion

		// Create a render target of dimensions equal to the viewport.
		public IRenderTarget CreateRenderTarget()
		{
			return _renderTargets.AddAndReturn(new RenderTargetHandle(
				new SharpDX.DXGI.Rational(1, 1), new SharpDX.DXGI.Rational(1, 1), true));
		}

		// Create a 2D texture of the given size and format, and fill it with the given data.
		public ITexture2D CreateTexture2D(int width, int height, Format format, dynamic contents, bool generateMips = false)
		{
			return AddResource(Texture.CreateFromScript(_device.Device, width, height, format, contents, generateMips));
		}

		// Load a texture from disk.
		public ITexture2D LoadTexture(string path, object generateMips = null)
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
				texture = Texture.LoadFromFile(_device.Device, absPath, mipGenerationMode, _logLogger);
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

			return AddResource(texture);
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
			_device = null;
		}

		// Called after script has finished executing (successfull).
		public Task ScriptExecutionComplete(IProgress progress)
		{
			progress.Update("Compiling shdaers...");
			return CompileShaders();
		}

		// Compile all shaders once we're done executing the script.
		private async Task CompileShaders()
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
					// It's ok to evaluate user variables during the frame callback.
					_isUserVarEvalSafe = true;

					frameCallback(renderContext);
				}
				finally
				{
					_isUserVarEvalSafe = false;
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

			// Update default depth buffer too.
			DepthBufferHandle.Default.DepthBuffer = viewInfo.DepthBuffer;
		}

		// Register a resource for later disposal, returning it for easy chaining.
		private T AddResource<T>(T resource) where T : IDisposable
		{
			_resources.Add(resource);
			return resource;
		}

		public dynamic GetScene() => Scene;

		public IDepthBuffer DepthBuffer => DepthBufferHandle.Default;
		public IDepthBuffer NoDepthBuffer => DepthBufferHandle.Null;

		public ITexture2D BlackTexture => _device.GlobalResources.BlackTexture;
		public ITexture2D WhiteTexture => _device.GlobalResources.WhiteTexture;
		public ITexture2D DefaultNormalTexture => _device.GlobalResources.DefaultNormalTexture;


		public Scene Scene { get; set; }

		private RenderDevice _device;

		// List of shaders. Needed to gather user properties.
		private List<Shader> shaders = new List<Shader>();
		private List<ShaderHandle> _shaderHandles = new List<ShaderHandle>();

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

		private readonly MipGenerator _mipGenerator;

		private readonly ILogger _logLogger;
		private readonly ILogger _scriptLogger;
		private readonly ILogger _shaderCompileLogger;
	}
}
