using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SRPRendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Device = SlimDX.Direct3D11.Device;

namespace SRPTests.TestRenderer
{
	class TestRenderer : IDisposable
	{
		private readonly Device device;
		private readonly Texture2D renderTargetTexture;
		private readonly RenderTargetView renderTarget;
		private readonly DepthBuffer depthBuffer;
		private readonly Texture2D stagingTexture;
		CompositeDisposable disposables;

		private readonly int _width = 256;
		private readonly int _height = 256;

		public Device Device { get { return device; } }

		public TestRenderer(int width, int height)
		{
			_width = width;
			_height = height;

			// Create a device without a swap chain for headless rendering.
			device = new Device(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport);

			// Create a render target to act as the back buffer.
			var rtDesc = new Texture2DDescription()
			{
				Width = _width,
				Height = _height,
				Format = SlimDX.DXGI.Format.B8G8R8A8_UNorm,
				MipLevels = 1,
				ArraySize = 1,
				BindFlags = BindFlags.RenderTarget,
				Usage = ResourceUsage.Default,
				CpuAccessFlags = CpuAccessFlags.None,
				SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0)
			};
			renderTargetTexture = new Texture2D(device, rtDesc);

			// Create the render target view.
			renderTarget = new RenderTargetView(device, renderTargetTexture);

			// Create a staging texture to copy render target to.
			rtDesc.BindFlags = BindFlags.None;
			rtDesc.CpuAccessFlags = CpuAccessFlags.Read;
			rtDesc.Usage = ResourceUsage.Staging;
			stagingTexture = new Texture2D(device, rtDesc);

			// Create a depth buffer.
			depthBuffer = new DepthBuffer(device, _width, _height);

			disposables = new CompositeDisposable(device, renderTarget, renderTargetTexture, depthBuffer, stagingTexture);
        }

		public void Dispose()
		{
			disposables.Dispose();
		}

		public Bitmap Render(ScriptRenderControl src)
		{
			Assert.NotNull(src);

			var context = device.ImmediateContext;

			// The SRC should clear the render target, so clear to a nice garish magenta so we detect if it doesn't.
			context.ClearRenderTargetView(renderTarget, new Color4(1.0f, 1.0f, 0.0f, 1.0f));

			// Clear back and depth buffers to ensure independence of tests.
			context.ClearDepthStencilView(depthBuffer.DSV, DepthStencilClearFlags.Depth, 1.0f, 0);

			// Construct view info object.
			// TODO: What about the camera?
			ViewInfo viewInfo = new ViewInfo(
				Matrix.Identity,
				Matrix.Identity,
				Vector3.Zero,
				1.0f,
				1000.0f,
				_width,
				_height,
				renderTarget,
				depthBuffer
				);

			src.Render(context, viewInfo);
			context.Flush();

			// Read back the render target and convert to bitmap.
			var bitmap = ReadBackBufferBitmap();

			// Quick hack to write out the image to a file to see if it's working.
			//bitmap.Save(System.IO.Path.Combine(
			//	Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "TestRender.png"),
			//	System.Drawing.Imaging.ImageFormat.Png);

			return bitmap;
        }

		// Read contents of backbuffer to into bitmap.
		private System.Drawing.Bitmap ReadBackBufferBitmap()
		{
			var context = device.ImmediateContext;

			// Copy to staging resource.
			context.CopyResource(renderTargetTexture, stagingTexture);

			var dataBox = context.MapSubresource(stagingTexture, 0, 0, MapMode.Read, SlimDX.Direct3D11.MapFlags.None);
			try
			{
				var result = new System.Drawing.Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bits = result.LockBits(new System.Drawing.Rectangle(0, 0, _width, _height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var destPtr = bits.Scan0;

				var lineSize = _width * 4;
				var lineTemp = new byte[lineSize];

				for (int y = 0; y < _height; y++)
				{
					dataBox.Data.Seek(y * dataBox.RowPitch, System.IO.SeekOrigin.Begin);
					dataBox.Data.ReadRange(lineTemp, 0, lineSize);

					Marshal.Copy(lineTemp, 0, destPtr, lineSize);
					destPtr += bits.Stride;
				}

				result.UnlockBits(bits);

				return result;
			}
			finally
			{
				context.UnmapSubresource(stagingTexture, 0);
			}
		}
	}
}
