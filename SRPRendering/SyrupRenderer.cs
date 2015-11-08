﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using SlimDX;
using SlimDX.Direct3D11;
using SRPCommon.Interfaces;
using SRPCommon.Scene;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using SRPCommon.Util;
using SRPScripting;
using Castle.DynamicProxy;

namespace SRPRendering
{
	// Central render loop logic.
	public class SyrupRenderer : IDisposable, IPropertySource
	{
		public SyrupRenderer(IWorkspace workspace, RenderDevice device, Scripting scripting)
		{
			_workspace = workspace;
			_device = device;

			PropertiesChanged = Observable.FromEventPattern(_properties, nameof(_properties.CollectionChanged))
				.Select(evt => _properties);

			// Create object for interacting with script.
			_scriptRenderControl = new ScriptRenderControl(workspace, device);
			_disposables.Add(_scriptRenderControl);

			// Generate wrapper proxy using Castle Dynamic Proxy to avoid direct script access to our internals.
			ScriptInterface = new ProxyGenerator().CreateInterfaceProxyWithTarget<IRenderInterface>(_scriptRenderControl);

			// Reset before script execution.
			_disposables.Add(scripting.PreExecute.Subscribe(PreExecuteScript));
			_disposables.Add(scripting.ExecutionComplete.Subscribe(ExecutionComplete));

			_overlayRenderer = new OverlayRenderer(device.GlobalResources);
		}

		private void PreExecuteScript(Script script)
		{
			Reset();
			_currentScript = script;
		}

		private void Reset()
		{
			_scriptRenderControl.Reset();
			_properties.Clear();

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
				// Get properties from script render control.
				var newProperties = _scriptRenderControl.GetProperties();

				// Add to list.
				// TODO: No need for this to be an observable collection.
				foreach (var prop in newProperties)
				{
					_properties.Add(prop);
				}
			}
			catch (ScriptException ex)
			{
				ScriptHelper.Instance.LogScriptError(ex);
				bScriptExecutionError = true;
				return;
			}

			foreach (var property in _properties)
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
			foreach (var property in _properties)
			{
				_disposables.Add(property.Subscribe(_ => FireRedrawRequired()));
			}

			bScriptExecutionError = !bSuccess;
		}

		public IObservable<Unit> RedrawRequired => _redrawRequired;
		private readonly Subject<Unit> _redrawRequired = new Subject<Unit>();
		private bool _bIgnoreRedrawRequests;

		// TODO: Make better.
		private void FireRedrawRequired()
		{
			if (!this._bIgnoreRedrawRequests)
			{
				_redrawRequired.OnNext(Unit.Default);
			}
		}

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
					FireRedrawRequired();
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
				deviceContext.ClearRenderTargetView(viewInfo.BackBuffer, new Color4(0));

				// Let the script do its thing.
				_scriptRenderControl.Render(deviceContext, viewInfo, _renderScene);
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
			_overlayRenderer.Draw(deviceContext, _renderScene, viewInfo);

			_bIgnoreRedrawRequests = false;
		}

		// Wrapper class that gets given to the script, acting as a firewall to prevent it from accessing this class directly.
		public IRenderInterface ScriptInterface { get; }

		private ObservableCollection<IUserProperty> _properties = new ObservableCollection<IUserProperty>();
		public ObservableCollection<IUserProperty> Properties => _properties;
		IEnumerable<IUserProperty> IPropertySource.Properties => _properties;

		public IObservable<IEnumerable<IUserProperty>> PropertiesChanged { get; }

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
	}
}