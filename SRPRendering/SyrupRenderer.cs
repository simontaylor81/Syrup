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

namespace SRPRendering
{
	// Central render loop logic.
	public class SyrupRenderer : IDisposable, IPropertySource
	{
		public SyrupRenderer(IWorkspace workspace, RenderDevice device, Scripting scripting)
		{
			_workspace = workspace;
			_device = device;
			_scripting = scripting;

			// Create object for interacting with script.
			_scriptRenderControl = new ScriptRenderControl(workspace, device);
			_disposables.Add(_scriptRenderControl);

			// Generate wrapper proxy using Castle Dynamic Proxy to avoid direct script access to our internals.
			ScriptInterface = new ProxyGenerator().CreateInterfaceProxyWithTarget<IRenderInterface>(_scriptRenderControl);

			// Reset before script execution.
			if (scripting != null)
			{
				_disposables.Add(scripting.PreExecute.Subscribe(PreExecuteScript));
				_disposables.Add(scripting.ExecutionComplete.Subscribe(ExecutionComplete));
			}

			_overlayRenderer = new OverlayRenderer(device.GlobalResources);

			// Merge together change events of all current properties and fire redraw.
			_disposables.Add(PropertiesObservable
				.Select(props => props.Merge())
				.Switch()
				.Subscribe(_redrawRequired));
		}

		private void PreExecuteScript(Script script)
		{
			Reset();
			_currentScript = script;
		}

		private void Reset()
		{
			_scriptRenderControl.Reset();
			Properties = null;

			// Reset output logger so warnings are written again.
			OutputLogger.Instance.ResetLogOnce();
		}

		private void ExecutionComplete(Exception exception)
		{
			bScriptRenderError = false;

			var script = _currentScript;
			_currentScript = null;

			try
			{
				// Get properties from script render control.
				Properties = _scriptRenderControl.GetProperties();
			}
			catch (ScriptException ex)
			{
				LogScriptError(ex);
				bScriptExecutionError = true;
				return;
			}

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

			bScriptExecutionError = exception != null;
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
					_scriptRenderControl.Scene = value;

					// Dispose of the old render scene.
					DisposableUtil.SafeDispose(_renderScene);

					// Create new one.
					_renderScene = new RenderScene(_scene, _device);

					// Missing scene can cause rendering to fail -- give it another try with the new one.
					bScriptRenderError = false;
					_redrawRequired.OnNext(Unit.Default);
				}
			}
		}

		public void Dispose()
		{
			Reset();

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

			try
			{
				// Always clear the back buffer to black to avoid the script having to do so for trivial stuff.
				deviceContext.ClearRenderTargetView(viewInfo.BackBuffer, new RawColor4());

				// Let the script do its thing.
				_scriptRenderControl.Render(deviceContext, viewInfo, _renderScene);
			}
			catch (Exception ex)
			{
				LogScriptError(ex);

				// Remember that the script fails so we don't just fail over and over.
				bScriptRenderError = true;
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
			OutputLogger.Instance.LogLine(LogCategory.Script, "Script execution failed.");

			if (_scripting != null)
			{
				OutputLogger.Instance.Log(LogCategory.Script, _scripting.FormatScriptError(ex));
			}
			else
			{
				OutputLogger.Instance.LogLine(LogCategory.Script, ex.Message);
				OutputLogger.Instance.LogLine(LogCategory.Script, ex.StackTrace);
			}
		}

		// Wrapper class that gets given to the script, acting as a firewall to prevent it from accessing this class directly.
		public IRenderInterface ScriptInterface { get; }

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

		private bool bScriptExecutionError = false;		// True if there was a problem executing the script
		private bool bScriptRenderError = false;        // True if there was a script error while rendering

		// If true, previous rendering failed with a script problem, so we don't keep re-running until the script is fixed & re-run.
		public bool HasScriptError => bScriptExecutionError || bScriptRenderError;

		// Object that handles rendering the viewport overlay.
		private OverlayRenderer _overlayRenderer;

		// Renderer representation of the scene we're currently rendering
		private RenderScene _renderScene;

		// Original scene data the above was created from.
		private Scene _scene;

		// Script currently being executed.
		private Script _currentScript;

		// List of things to dispose.
		private CompositeDisposable _disposables = new CompositeDisposable();

		private readonly ScriptRenderControl _scriptRenderControl;
		private readonly Scripting _scripting;
	}
}
