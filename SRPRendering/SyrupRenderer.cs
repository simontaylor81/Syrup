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
				.Select(props => props.Merge())
				.Switch()
				.Subscribe(_redrawRequired));
		}

		// Execute a script using this renderer.
		public async Task ExecuteScript(Script script, IProgress progress)
		{
			Trace.Assert(script != null);
			Trace.Assert(progress != null);

			// Clear output from previous runs.
			// Clear script last so we select that in the output window.
			_shaderCompileLogger.Clear();
			_scriptLogger.Clear();

			// Don't run if we're already running a script.
			if (!_bInProgress)
			{
				_bInProgress = true;

				// Create object for interacting with script.
				_scriptRenderControl.Ref = new ScriptRenderControl(_workspace, _device, _loggerFactory);
				_scriptRenderControl.Ref.Scene = Scene;

				// Generate wrapper proxy using Castle Dynamic Proxy to avoid direct script access to our internals.
				_scriptInterface = new ProxyGenerator().CreateInterfaceProxyWithTarget<IRenderInterface>(_scriptRenderControl.Ref);

				PreExecuteScript();

				try
				{
					progress.Update("Running script...");

					// Compile and run script.
					var compiledScript = await _scripting.Compile(script);
					await compiledScript.ExecuteAsync(_scriptInterface);

					await PostExecuteScript(script, progress);
				}
				catch (Exception ex)
				{
					bScriptExecutionError = true;
					LogScriptError(ex);
					throw;
				}
				finally
				{
					_bInProgress = false;
				}
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

			// Attempt to copy over previous property values so they're not reset every
			// time the user re-runs the script.
			foreach (var property in Properties)
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

			bScriptExecutionError = false;
		}

		// Fire redraw when it's requested, except when disallowed.
		public IObservable<Unit> RedrawRequired => _redrawRequired.Where(_ => !_bIgnoreRedrawRequests);

		private readonly Subject<Unit> _redrawRequired = new Subject<Unit>();
		private bool _bIgnoreRedrawRequests;

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

			if (_scriptRenderControl.Ref != null)
			{
				try
				{
					// Let the script do its thing.
					_scriptRenderControl.Ref.Render(deviceContext, viewInfo, _renderScene);
				}
				catch (Exception ex)
				{
					LogScriptError(ex);

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

		private void LogScriptError(Exception ex)
		{
			_scriptLogger.LogLine("Script execution failed.");
			_scriptLogger.Log(_scripting.FormatScriptError(ex));
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

		// Wrapper class that gets given to the script, acting as a firewall to prevent it from accessing this class directly.
		private IRenderInterface _scriptInterface;

		private readonly Scripting _scripting;
		private bool _bInProgress;

		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger _scriptLogger;
		private readonly ILogger _shaderCompileLogger;
	}
}
