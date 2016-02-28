using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	// Encapsulation of a D3D render target.
	class RenderTarget : IDisposable
	{
		public Texture2D Texture2D => texture;
		public RenderTargetView RTV => rtv;
		public ShaderResourceView SRV => srv;

		public int Width => texture.Description.Width;
		public int Height => texture.Description.Height;

		public RenderTarget(Device device, int width, int height, SharpDX.DXGI.Format format)
		{
			// Create the texture itself.
			var desc = new Texture2DDescription()
				{
					Width = width,
					Height = height,
					Format = format,
					MipLevels = 1,
					ArraySize = 1,
					BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
					Usage = ResourceUsage.Default,
					CpuAccessFlags = CpuAccessFlags.None,
					SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
				};
			texture = new Texture2D(device, desc);

			// Create the render target view.
			rtv = new RenderTargetView(device, texture);

			// Create the shader resource so the RT can be read from a shader.
			srv = new ShaderResourceView(device, texture);
		}

		public void Dispose()
		{
			srv.Dispose();
			rtv.Dispose();
			texture.Dispose();
		}

		private Texture2D texture;
		private RenderTargetView rtv;
		private ShaderResourceView srv;
	}
}
