using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using SlimDX.Direct3D11;
using SRPCommon.Util;
using System.Reactive.Disposables;

namespace SRPRendering
{
	public interface IGlobalResources : IDisposable
	{
		// Texture to use to indicate error when non is found.
		Texture ErrorTexture { get; }

		IDrawable CubeMesh { get; }
		IDrawable PlaneMesh { get; }
		IDrawable SphereMesh { get; }
		IDrawable FullscreenQuad { get; }

		IBasicShaders BasicShaders { get; }
		IShaderCache ShaderCache { get; }

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
		public Texture ErrorTexture { get; }

		public IDrawable CubeMesh { get; }
		public IDrawable PlaneMesh { get; }
		public IDrawable SphereMesh { get; }
		public IDrawable FullscreenQuad { get; }

		public IBasicShaders BasicShaders { get; }
		public IShaderCache ShaderCache { get; }

		// State object caches.
		public IStateObjectCache<RasterizerState, RasterizerStateDescription> RastStateCache { get; }
		public IStateObjectCache<DepthStencilState, DepthStencilStateDescription> DepthStencilStateCache { get; }
		public IStateObjectCache<BlendState, BlendStateDescription> BlendStateCache { get; }
		public IStateObjectCache<SamplerState, SamplerDescription> SamplerStateCache { get; }
		public IInputLayoutCache InputLayoutCache { get; }

		private CompositeDisposable disposables = new CompositeDisposable();

		// Initialise the resources.
		public GlobalResources(Device device)
		{
			// Create constant pink error texture.
			ErrorTexture = CreateConstantColourTexture(device, Color.Magenta);
			disposables.Add(ErrorTexture);

			// Create simple utility meshes.
			var cubeMesh = BasicMesh.CreateCube(device);
			CubeMesh = cubeMesh;
			disposables.Add(cubeMesh);

			var planeMesh = BasicMesh.CreatePlane(device);
			PlaneMesh = planeMesh;
			disposables.Add(planeMesh);

			var sphereMesh = BasicMesh.CreateSphere(device, 12, 6);
			SphereMesh = sphereMesh;
			disposables.Add(sphereMesh);

			var fullscreenQuad = new FullscreenQuad(device);
			FullscreenQuad = fullscreenQuad;
			disposables.Add(fullscreenQuad);

			BasicShaders = new BasicShaders(device);
			disposables.Add(BasicShaders);

			ShaderCache = new ShaderCache(device);
			disposables.Add(ShaderCache);

			// Create the state object caches.
			RastStateCache = new StateObjectCache<RasterizerState, RasterizerStateDescription>(device, RasterizerState.FromDescription);
			DepthStencilStateCache = new StateObjectCache<DepthStencilState, DepthStencilStateDescription>(device, DepthStencilState.FromDescription);
			BlendStateCache = new StateObjectCache<BlendState, BlendStateDescription>(device, BlendState.FromDescription);
			SamplerStateCache = new StateObjectCache<SamplerState, SamplerDescription>(device, SamplerState.FromDescription);
			InputLayoutCache = new InputLayoutCache();

			disposables.Add(RastStateCache);
			disposables.Add(DepthStencilStateCache);
			disposables.Add(BlendStateCache);
			disposables.Add(SamplerStateCache);
			disposables.Add(InputLayoutCache);
		}

		// Release all resources.
		public void Dispose()
		{
			disposables.Dispose();
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
