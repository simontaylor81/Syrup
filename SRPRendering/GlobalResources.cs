using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using SlimDX.Direct3D11;
using SRPCommon.Util;

namespace SRPRendering
{
	public interface IGlobalResources : IDisposable
	{
		// The resources themselves.
		Texture ErrorTexture { get; }

		// State object caches.
		IStateObjectCache<RasterizerState, RasterizerStateDescription> RastStateCache { get; }
		IStateObjectCache<DepthStencilState, DepthStencilStateDescription> DepthStencilStateCache { get; }
		IStateObjectCache<BlendState, BlendStateDescription> BlendStateCache { get; }
		IStateObjectCache<SamplerState, SamplerDescription> SamplerStateCache { get; }
		IInputLayoutCache InputLayoutCache { get; }
	}

	// Holder for various global D3D resources.
	internal class GlobalResources : IGlobalResources
	{
		// The resources themselves.
		public Texture ErrorTexture { get; private set; }

		// State object caches.
		public IStateObjectCache<RasterizerState, RasterizerStateDescription> RastStateCache { get; private set; }
		public IStateObjectCache<DepthStencilState, DepthStencilStateDescription> DepthStencilStateCache { get; private set; }
		public IStateObjectCache<BlendState, BlendStateDescription> BlendStateCache { get; private set; }
		public IStateObjectCache<SamplerState, SamplerDescription> SamplerStateCache { get; private set; }
		public IInputLayoutCache InputLayoutCache { get; private set; }

		// Initialise the resources.
		public GlobalResources(Device device)
		{
			// Create constant pink error texture.
			ErrorTexture = CreateConstantColourTexture(device, Color.Magenta);

			// Create the state object caches.
			RastStateCache = new StateObjectCache<RasterizerState, RasterizerStateDescription>(device, RasterizerState.FromDescription);
			DepthStencilStateCache = new StateObjectCache<DepthStencilState, DepthStencilStateDescription>(device, DepthStencilState.FromDescription);
			BlendStateCache = new StateObjectCache<BlendState, BlendStateDescription>(device, BlendState.FromDescription);
			SamplerStateCache = new StateObjectCache<SamplerState, SamplerDescription>(device, SamplerState.FromDescription);
			InputLayoutCache = new InputLayoutCache();
		}

		// Release all resources.
		public void Dispose()
		{
			DisposableUtil.SafeDispose(ErrorTexture);
			ErrorTexture = null;

			RastStateCache.Dispose();
			DepthStencilStateCache.Dispose();
			BlendStateCache.Dispose();
			SamplerStateCache.Dispose();
			InputLayoutCache.Dispose();
		}

		// Create a texture with a solid colour.
		private Texture CreateConstantColourTexture(Device device, Color colour)
		{
			// Make a 1x1 texture.
			var description = new Texture2DDescription()
			{
				Width = 1,
				Height = 1,
				Format = SlimDX.DXGI.Format.R8G8B8A8_UNorm_SRGB,
				MipLevels = 1,
				SampleDescription = new SlimDX.DXGI.SampleDescription() { Count = 1 },
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				Usage = ResourceUsage.Default
			};

			// Initilise with the constant colour.
			var data = new[] {colour.R, colour.G, colour.B, colour.A };
			var dataStream = new SlimDX.DataStream(data, true, true);
			var dataRect = new SlimDX.DataRectangle(4, dataStream);

			// Create the texture resource.
			var texture2D = new Texture2D(device, description, dataRect);

			// Create the shader resource view.
			var srv = new ShaderResourceView(device, texture2D);

			return new Texture(texture2D, srv);
		}
	}
}
