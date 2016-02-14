﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Interfaces;
using SRPScripting;

namespace SRPRendering
{
	// Class used to generate texture mip maps using custom shader code.
	class MipGenerator : IDisposable
	{
		private readonly RenderDevice _device;
		private readonly IWorkspace _workspace;
		private readonly IShader _vertexShader;

		public MipGenerator(RenderDevice device, IWorkspace workspace)
		{
			_device = device;
			_workspace = workspace;

			// Compile vertex shader.
			_vertexShader = device.GlobalResources.ShaderCache.GetShader(
				RenderUtils.GetShaderFilename("GenMipsVS.hlsl"), "Main", "vs_4_0", RenderUtils.GetShaderFilename, null);
		}

		// Do the generation.
		public void Generate(Texture texture, string shaderFile)
		{
			// TODO: Skip and warn if texture is compressed.

			// Custom include handler to include the special custom code from the script.
			Func<string, string> includeHandler = filename =>
			{
				// Handle system includes first, otherwise project files could accidentally override them.
				var systemFile = RenderUtils.GetShaderFilename(filename);
				if (File.Exists(systemFile))
				{
					return systemFile;
				}

				// Is it the special include?
				if (filename == "_scriptDownsample")
				{
					filename = shaderFile;
				}

				// Look for the file in the project.
				return _workspace.FindProjectFile(filename);
			};

			// Compile pixel shader.
			var pixelShader = _device.GlobalResources.ShaderCache.GetShader(
				RenderUtils.GetShaderFilename("GenMipsPS.hlsl"), "Main", "ps_4_0", includeHandler, null);
			var destMipVariable = pixelShader.FindVariable("DestMip");

			var texDesc = texture.Texture2D.Description;
			int mipWidth = texDesc.Width >> 1;
			int mipHeight = texDesc.Height >> 1;

			// Allocate intermediate render target is big enough for the first mip.
			using (var renderTarget = new RenderTarget(_device.Device, mipWidth, mipHeight, texture.SRV.Description.Format))
			{
				// Use immediate context for drawing.
				var context = _device.Device.ImmediateContext;

				// Set common state.
				SetCommonState(context);
				pixelShader.Set(context);
				context.OutputMerger.SetTargets(renderTarget.RTV);

				BindResources(pixelShader, texture);

				int mip = 1;
				while (mipWidth > 0 && mipHeight > 0)
				{
					context.Rasterizer.SetViewports(new SlimDX.Direct3D11.Viewport(0, 0, mipWidth, mipHeight));

					destMipVariable?.Set(mip);
					pixelShader.UpdateVariables(context, null, null, null, null);

					// Render 'fullscreen' quad to downsample the mip.
					_device.GlobalResources.FullscreenQuad.Draw(context);

					// Copy result back to the mip chain of the source texture.
					var region = new SlimDX.Direct3D11.ResourceRegion(0, 0, 0, mipWidth, mipHeight, 1);
					context.CopySubresourceRegion(renderTarget.Texture2D, 0, region, texture.Texture2D, mip, 0, 0, 0);

					// Move to the next mip.
					mipWidth >>= 1;
					mipHeight >>= 1;
					mip++;
				}
			}
		}

		// Set render state that is always the same, independent of shader, texture, etc.
		private void SetCommonState(SlimDX.Direct3D11.DeviceContext context)
		{
			context.Rasterizer.State = _device.GlobalResources.RastStateCache.Get(RastState.Default.ToD3D11());
			context.OutputMerger.DepthStencilState = _device.GlobalResources.DepthStencilStateCache.Get(DepthStencilState.DisableDepth.ToD3D11());
			context.OutputMerger.BlendState = _device.GlobalResources.BlendStateCache.Get(BlendState.NoBlending.ToD3D11());

			// Set input layout
			context.InputAssembler.InputLayout = _device.GlobalResources.InputLayoutCache.GetInputLayout(
				_device.Device, _vertexShader.Signature, FullscreenQuad.InputElements);

			_vertexShader.Set(context);
		}

		// Bind texture and samplers.
		private void BindResources(IShader ps, Texture texture)
		{
			// Bind the texture itself.
			var texVariable = ps.FindResourceVariable("Texture");
			if (texVariable != null)
			{
				texVariable.Resource = texture.SRV;
			}

			// Bind samplers.
			SetSampler(ps, "LinearSampler", SamplerState.LinearClamp);
			SetSampler(ps, "PointSampler", SamplerState.PointClamp);
		}

		// Simpler helper for setting samplers by name using a state from the cache.
		private void SetSampler(IShader shader, string name, SamplerState state)
		{
			var sampler = shader.FindSamplerVariable(name);
			if (sampler != null)
			{
				sampler.State = _device.GlobalResources.SamplerStateCache.Get(state.ToD3D11());
			}
		}

		// TODO: Remove if this turns out to be unnecesary.
		public void Dispose()
		{
		}
	}
}