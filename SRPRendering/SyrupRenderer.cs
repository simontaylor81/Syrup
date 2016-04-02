using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using SharpDX;
using SharpDX.Direct3D11;
using SRPCommon.Interfaces;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using SRPCommon.Util;
using SRPScripting;
using Castle.DynamicProxy;
using SharpDX.Mathematics.Interop;
using System.Threading.Tasks;
using System.Diagnostics;
using SRPCommon.Logging;

namespace SRPRendering
{
	// Central render loop logic.
	public class SyrupRenderer : IDisposable, IPropertySource
	{
		public SyrupRenderer(IWorkspace workspace, RenderDevice device, Scripting scripting, ILoggerFactory loggerFactory)
		{
			Trace.Assert(workspace != null);
			Trace.Assert(device != null);
			Trace.Assert(scripting != null);

			_workspace = workspace;
			_device = device;
			_scripting = scripting;

			_disposables.Add(_scriptRenderControl);

			_loggerFactory = loggerFactory;
			_scriptLogger = loggerFactory.CreateLogger("Script");
			_shaderCompileLogger = loggerFactory.CreateLogger("ShaderCompile");

			_overlayRenderer = new OverlayRenderer(device.GlobalResources, _scriptLogger);

			// Merge together change events of all current properties and fire redraw.
			_disposables.Add(PropertiesObservable
				.Select(props => props.Where(p => !p.RequiresReExecute))
				.Select(props => props.Merge())
				.Switch()
				.Subscribe(_redrawRequired));

			// Fire re-execute when the subset of properties that need it change.
			ReExecuteRequired = PropertiesObservable
				.Select(props => props.Where(p => p.RequiresReExecute))
				.Select(props => props.Merge())
				.Switch();
		}

		// Execute a script using this renderer.
		public async Task ExecuteScript(Script script, IProgress progress)
		{
			Trace.Assert(script != null);
			Trace.Assert(progress != null);

			// Should never try to run if we're already running.
			Trace.Assert(!_bInProgress);
			_bInProgress = true;
			try
			{
				// Clear output from previous runs.
				// Clear script last so we select that in the output window.
				_shaderCompileLogger.Clear();
				_scriptLogger.Clear();

				PreExecuteScript();

				// Try to compile the script.
				ICompiledScript compiledScript;
				try
				{
					compiledScript = await _scripting.Compile(script);
				}
				catch (Exception ex)
				{
					bScriptExecutionError = true;
					_scriptLogger.LogLine("Failed to compile script");
					_scriptLogger.Log(ex.Message);
					throw;
				}

				// Execute it.
				await ExecuteCompiledScript(script, compiledScript, progress);

				// Execution successful -- remember what we ran.
				_previousScript = script;
				_previousCompiledScript = compiledScript;
			}
			finally
			{
				_bInProgress = false;
			}
		}

		// Re-execute the exact same script as the last run, without recompiling.
		public async Task ReExecuteScript(IProgress progress)
		{
			Trace.Assert(progress != null);

			// Must have executed something to re-execute it.
			Trace.Assert(_previousScript != null);
			Trace.Assert(_previousCompiledScript != null);

			// Should never try to run if we're already running.
			Trace.Assert(!_bInProgress);
			_bInProgress = true;
			try
			{
				// *Don't* clear logs for reruns.

				PreExecuteScript();
				await ExecuteCompiledScript(_previousScript, _previousCompiledScript, progress);
			}
			finally
			{
				_bInProgress = false;
			}
		}

		private async Task ExecuteCompiledScript(Script script, ICompiledScript compiledScript, IProgress progress)
		{
			// Create object for interacting with script.
			_scriptRenderControl.Ref = new ScriptRenderControl(_workspace, _device, _loggerFactory, script.UserProperties);
			_scriptRenderControl.Ref.Scene = Scene;

			// Generate wrapper proxy using Castle Dynamic Proxy to avoid direct script access to our internals.
			var scriptInterface = new ProxyGenerator().CreateInterfaceProxyWithTarget<IRenderInterface>(_scriptRenderControl.Ref);

			try
			{
				progress.Update("Running script...");

				await compiledScript.ExecuteAsync(scriptInterface);
				await PostExecuteScript(script, progress);
			}
			catch (Exception ex)
			{
				bScriptExecutionError = true;
				LogScriptError(ex, compiledScript);
				throw;
			}
		}

		private void PreExecuteScript()
		{
			bScriptRenderError = false;
			Properties = null;
		}

		private async Task PostExecuteScript(Script script, IProgress progress)
		{
			// Tell the script render control that we're done,
			// so it can compile shaders, etc.
			await _scriptRenderControl.Ref.ScriptExecutionComplete(progress);

			// Get properties from script render control.
			progress.Update("Gathering properties");
			Properties = _scriptRenderControl.Ref.GetProperties();

			// Save user properties for use in subsequent runs.
			foreach (var property in Properties)
			{
				script.UserProperties[property.Name] = property;
			}

			bScriptExecutionError = false;
		}

		// Fire redraw when it's requested, except when disallowed.
		public IObservable<Unit> RedrawRequired => _redrawRequired.Where(_ => !_bIgnoreRedrawRequests);

		private readonly Subject<Unit> _redrawRequired = new Subject<Unit>();
		private bool _bIgnoreRedrawRequests;

		public IObservable<Unit> ReExecuteRequired { get; }

		public Scene Scene
		{
			get { return _scene; }
			set
			{
				if (_scene != value)
				{
					_scene = value;

					if (_scriptRenderControl.Ref != null)
					{
						_scriptRenderControl.Ref.Scene = value;
					}

					// Dispose of the old render scene.
					DisposableUtil.SafeDispose(_renderScene);

					// Create new one.
					_renderScene = new RenderScene(_scene, _device, _loggerFactory);

					// Missing scene can cause rendering to fail -- give it another try with the new one.
					bScriptRenderError = false;
					_redrawRequired.OnNext(Unit.Default);
				}
			}
		}

		public void Dispose()
		{
			_disposables.Dispose();
			DisposableUtil.SafeDispose(_renderScene);
			_device = null;
		}

		public void Render(DeviceContext deviceContext, ViewInfo viewInfo)
		{
			// Bail if there was a problem with the scripts.
			if (HasScriptError)
			{
				return;
			}

			// Don't redraw viewports because of internal shader variable changes (e.g. bindings).
			_bIgnoreRedrawRequests = true;

			// Always clear the back buffer to black to avoid the script having to do so for trivial stuff.
			deviceContext.ClearRenderTargetView(viewInfo.BackBuffer, new RawColor4());

			if (_scriptRenderControl.Ref != null && _previousCompiledScript != null)
			{
				try
				{
					// Let the script do its thing.
					_scriptRenderControl.Ref.Render(deviceContext, viewInfo, _renderScene);
				}
				catch (Exception ex)
				{
					LogScriptError(ex, _previousCompiledScript);

					// Remember that the script fails so we don't just fail over and over.
					bScriptRenderError = true;
				}
			}

			// Make sure we're rendering to the back buffer before rendering the overlay.
			deviceContext.OutputMerger.SetTargets(viewInfo.DepthBuffer.DSV, viewInfo.BackBuffer);
			deviceContext.Rasterizer.SetViewports(new[] { new RawViewportF
			{
				X = 0.0f,
				Y = 0.0f,
				Width = viewInfo.ViewportWidth,
				Height = viewInfo.ViewportHeight,
				MinDepth = 0.0f,
				MaxDepth = 0.0f,
			} });

			// Render the overlay.
			_overlayRenderer.Draw(deviceContext, _renderScene, viewInfo);

			_bIgnoreRedrawRequests = false;
		}

		private void LogScriptError(Exception ex, ICompiledScript compiledScript)
		{
			_scriptLogger.LogLine("Script execution failed.");
			_scriptLogger.Log(compiledScript.FormatError(ex));
		}

		// User properties exposed by the script.
		private IEnumerable<IUserProperty> _properties = Enumerable.Empty<IUserProperty>();
		public IEnumerable<IUserProperty> Properties
		{
			get { return _properties; }
			private set
			{
				if (_properties != value)
				{
					_properties = value.EmptyIfNull();
					_propertiesSubject.OnNext(_properties);
				}
			}
		}

		// Observable that fires whenever the set of user properties changes.
		private Subject<IEnumerable<IUserProperty>> _propertiesSubject = new Subject<IEnumerable<IUserProperty>>();
		public IObservable<IEnumerable<IUserProperty>> PropertiesObservable => _propertiesSubject;

		private RenderDevice _device;

		// Pointer back to the workspace. Needed so we can access the project to get shaders from.
		private IWorkspace _workspace;

		private bool bScriptExecutionError = false;     // True if there was a problem executing the script
		private bool bScriptRenderError = false;        // True if there was a script error while rendering

		// If true, previous rendering failed with a script problem, so we don't keep re-running until the script is fixed & re-run.
		public bool HasScriptError => bScriptExecutionError || bScriptRenderError;

		// Object that handles rendering the viewport overlay.
		private OverlayRenderer _overlayRenderer;

		// Renderer representation of the scene we're currently rendering
		private RenderScene _renderScene;

		// Original scene data the above was created from.
		private Scene _scene;

		// List of things to dispose.
		private CompositeDisposable _disposables = new CompositeDisposable();

		private DisposableRef<ScriptRenderControl> _scriptRenderControl;

		private readonly Scripting _scripting;
		private bool _bInProgress;

		private Script _previousScript;
		private ICompiledScript _previousCompiledScript;

		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger _scriptLogger;
		private readonly ILogger _shaderCompileLogger;
	}
}
