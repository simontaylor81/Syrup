using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	// Encapsulation of a D3D depth buffer.
	public class DepthBuffer : IDisposable
	{
		public DepthStencilView DSV => dsv;
		public ShaderResourceView SRV => srv;

		public int Width => texture.Description.Width;
		public int Height => texture.Description.Height;

		public DepthBuffer(Device device, int width, int height)
		{
			// Create depth/stencil buffer texture.
			var depthDesc = new Texture2DDescription()
				{
					Width = width,
					Height = height,
					MipLevels = 1,
					ArraySize = 1,
					Format = SharpDX.DXGI.Format.R24G8_Typeless,
					Usage = ResourceUsage.Default,
					BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)	// TODO MSAA?
				};
			texture = new Texture2D(device, depthDesc);

			// Create the view for the depth buffer.
			var dsvDesc = new DepthStencilViewDescription()
				{
					Dimension = DepthStencilViewDimension.Texture2D,
					Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
				};
			dsv = new DepthStencilView(device, texture, dsvDesc);

			// Create the shader resource so the buffer can be read from a shader.
			var srvDesc = new ShaderResourceViewDescription();
			srvDesc.Dimension = ShaderResourceViewDimension.Texture2D;
			srvDesc.Format = SharpDX.DXGI.Format.R24_UNorm_X8_Typeless;
			srvDesc.Texture2D.MipLevels = 1;
			srvDesc.Texture2D.MostDetailedMip = 0;

			srv = new ShaderResourceView(device, texture, srvDesc);
		}

		public void Dispose()
		{
			srv.Dispose();
			dsv.Dispose();
			texture.Dispose();
		}

		private Texture2D texture;
		private DepthStencilView dsv;
		private ShaderResourceView srv;
	}
}
