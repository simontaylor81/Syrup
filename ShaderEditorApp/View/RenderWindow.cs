using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct3D11;

using SRPRendering;
using ShaderEditorApp.ViewModel;
using SharpDX.DXGI;

namespace ShaderEditorApp.View
{

	public class RenderWindow : Control
	{
		private List<IDisposable> resources = new List<IDisposable>();
		private List<IDisposable> sizeDependentResources = new List<IDisposable>();

		private readonly SharpDX.Direct3D11.Device _device;
		private readonly SwapChain swapChain;

		private readonly WorkspaceViewModel _workspaceVM;

		private RenderTargetView renderTarget;
		private DepthBuffer depthBuffer;

		private Camera camera;

		private bool bNeedsRepaint = false;

		public ViewportViewModel ViewportViewModel { get; }

		public RenderWindow(RenderDevice device, WorkspaceViewModel workspaceVM)
		{
			_device = device.Device;
			_workspaceVM = workspaceVM;
		
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

			var factory = device.Adapter.GetParent<Factory>();
			swapChain = new SwapChain(factory, device.Device, description);
			resources.Add(swapChain);

			// Prevent DXGI handling of alt+enter, which doesn't work properly with Winforms
			factory.MakeWindowAssociation(Handle, WindowAssociationFlags.IgnoreAltEnter);

			this.SizeChanged += OnResize;

			// Redraw viewports when required.
			_workspaceVM.Workspace.RedrawRequired.Subscribe(_ => Invalidate());
		}

		private void OnResize(object sender, EventArgs e)
		{
			// Release any existing resources.
			foreach (var obj in sizeDependentResources)
				obj.Dispose();
			sizeDependentResources.Clear();

			swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.AllowModeSwitch);
			using (var resource = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0))
			{
				renderTarget = new RenderTargetView(_device, resource);
			}
			sizeDependentResources.Add(renderTarget);

			// Create depth/stencil buffer texture.
			depthBuffer = new DepthBuffer(_device, Width, Height);

			sizeDependentResources.Add(depthBuffer);
		}

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

			_workspaceVM.Workspace.Renderer?.Render(context, viewInfo);

			swapChain.Present(0, PresentFlags.None);
		}

		public void Tick()
		{
			if (bNeedsRepaint || _workspaceVM.RealTimeMode)
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
