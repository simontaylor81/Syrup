using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using SlimDX.Direct3D11;

namespace ShaderEditorApp.Rendering
{
	// Holder for various global D3D resources.
	class GlobalResources : IDisposable
	{
		// The resources themselves.
		public Texture ErrorTexture { get; private set; }

		// State object caches.
		public StateObjectCache<RasterizerState, RasterizerStateDescription> RastStateCache { get; private set; }
		public StateObjectCache<DepthStencilState, DepthStencilStateDescription> DepthStencilStateCache { get; private set; }
		public StateObjectCache<BlendState, BlendStateDescription> BlendStateCache { get; private set; }
		public StateObjectCache<SamplerState, SamplerDescription> SamplerStateCache { get; private set; }

		// Singleton instance.
		private static Lazy<GlobalResources> instance = new Lazy<GlobalResources>(() => new GlobalResources());
		public static GlobalResources Instance { get { return instance.Value; } }

		// Private constructor to inforce singleton.
		private GlobalResources()
		{
		}

		// Initialise the resources.
		public void Init(Device device)
		{
			// Create constant pink error texture.
			ErrorTexture = CreateConstantColourTexture(device, Color.Magenta);

			// Create the state object caches.
			RastStateCache = new StateObjectCache<RasterizerState, RasterizerStateDescription>(device, RasterizerState.FromDescription);
			DepthStencilStateCache = new StateObjectCache<DepthStencilState, DepthStencilStateDescription>(device, DepthStencilState.FromDescription);
			BlendStateCache = new StateObjectCache<BlendState, BlendStateDescription>(device, BlendState.FromDescription);
			SamplerStateCache = new StateObjectCache<SamplerState, SamplerDescription>(device, SamplerState.FromDescription);
		}

		// Release all resources.
		public void Dispose()
		{
			RenderUtils.SafeDispose(ErrorTexture);
			ErrorTexture = null;

			RastStateCache.Dispose();
			DepthStencilStateCache.Dispose();
			BlendStateCache.Dispose();
			SamplerStateCache.Dispose();
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
