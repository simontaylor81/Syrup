using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using SlimDX.D3DCompiler;

using SRPRendering;
using ShaderEditorApp.ViewModel;

namespace ShaderEditorApp.View
{

	public class RenderWindow : Control
	{
		private List<IDisposable> resources = new List<IDisposable>();
		private List<IDisposable> sizeDependentResources = new List<IDisposable>();

		private SlimDX.Direct3D11.Device _device;
		private SlimDX.DXGI.SwapChain swapChain;
		private RenderTargetView renderTarget;

		private ScriptRenderControl scriptControl;
		private Camera camera;

		// TODO: Refactor so the device isn't owned by the viewport.
		public SlimDX.Direct3D11.Device Device => _device;

		// Should we render every frame?
		public bool RealTimeMode { get; set; }
		private bool bNeedsRepaint = false;

		internal ScriptRenderControl ScriptControl
		{
			get { return scriptControl; }
			set
			{
				// Only do this once.
				System.Diagnostics.Debug.Assert(scriptControl == null);
				System.Diagnostics.Debug.Assert(value != null);

				scriptControl = value;
				resources.Add(value);
			}
		}

		public ViewportViewModel ViewportViewModel { get; }

		public RenderWindow(SlimDX.Direct3D11.Device device)
		{
			_device = device;

			// Create camera.
			ViewportViewModel = new ViewportViewModel();
			camera = new Camera(this, ViewportViewModel);

			MouseClick += RenderWindow_MouseClick;

			// Initialise D3D11 swap chain.
			var description = new SwapChainDescription()
			{
				BufferCount = 1,
				Usage = Usage.RenderTargetOutput,
				OutputHandle = Handle,
				IsWindowed = true,
				ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
				SampleDescription = new SampleDescription(1, 0),
				Flags = SwapChainFlags.AllowModeSwitch,
				SwapEffect = SwapEffect.Discard
			};

			swapChain = new SwapChain(device.Factory, device, description);
			resources.Add(swapChain);

			// prevent DXGI handling of alt+enter, which doesn't work properly with Winforms
			device.Factory.SetWindowAssociation(Handle, WindowAssociationFlags.IgnoreAltEnter);

			this.SizeChanged += OnResize;
		}

		private void OnResize(object sender, EventArgs e)
		{
			// Release any existing resources.
			foreach (var obj in sizeDependentResources)
				obj.Dispose();
			sizeDependentResources.Clear();

			swapChain.ResizeBuffers(2, 0, 0, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowModeSwitch);
			using (var resource = SlimDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0))
				renderTarget = new RenderTargetView(_device, resource);
			sizeDependentResources.Add(renderTarget);

			// Create depth/stencil buffer texture.
			depthBuffer = new DepthBuffer(_device, Width, Height);

			sizeDependentResources.Add(depthBuffer);
		}

		private DepthBuffer depthBuffer;


		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// clean up all resources
				foreach (var resource in sizeDependentResources)
					resource.Dispose();
				foreach (var resource in resources)
					resource.Dispose();

				sizeDependentResources.Clear();
				resources.Clear();
			}

			base.Dispose(disposing);
		}

		private void Render()
		{
			var context = _device.ImmediateContext;

			// Clear depth buffer.
			context.ClearDepthStencilView(depthBuffer.DSV, DepthStencilClearFlags.Depth, 1.0f, 0);

			// Construct view info object.
			var viewInfo = new ViewInfo(
				camera.WorldToViewMatrix,
				camera.GetViewToProjectionMatrix(AspectRatio),
				camera.EyePosition,
				camera.Near,
				camera.Far,
				ClientSize.Width,
				ClientSize.Height,
				renderTarget,
				depthBuffer
				);

			if (scriptControl != null)
				scriptControl.Render(context, viewInfo);

			swapChain.Present(0, PresentFlags.None);
		}

		public void Tick()
		{
			if (bNeedsRepaint || RealTimeMode)
			{
				bNeedsRepaint = false;
				Render();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			bNeedsRepaint = true;
		}

		// Override background paint event to prevent flickering.
		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing -- Render() clears the whole window.
		}

		private float AspectRatio => (float)ClientSize.Width / (float)ClientSize.Height;

		void RenderWindow_MouseClick(object sender, MouseEventArgs e)
		{
			// Acquire focus when we're clicked on.
			Focus();
		}
	}
}
