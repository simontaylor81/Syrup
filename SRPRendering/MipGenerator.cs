﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using SRPScripting;

namespace SRPRendering
{
	// Class used to generate texture mip maps using custom shader code.
	class MipGenerator : IDisposable
	{
		private readonly RenderDevice _device;
		private readonly IShader _vertexShader;

		public MipGenerator(RenderDevice device)
		{
			_device = device;

			// Compile vertex shader.
			_vertexShader = device.GlobalResources.ShaderCache.GetShader(
				RenderUtils.GetShaderFilename("GenMipsVS.hlsl"), "Main", "vs_4_0", RenderUtils.GetShaderFilename, null);
		}

		// Do the generation.
		public void Generate(Texture texture)
		{
			// TODO: Skip and warn if texture is compressed.

			// Compile pixel shader.
			// TODO: Insert custom sampling code here.
			var pixelShader = _device.GlobalResources.ShaderCache.GetShader(
				RenderUtils.GetShaderFilename("GenMipsPS.hlsl"), "Main", "ps_4_0", RenderUtils.GetShaderFilename, null);

			var texDesc = texture.Texture2D.Description;
			int mipWidth = texDesc.Width >> 1;
			int mipHeight = texDesc.Height >> 1;

			// Allocate intermediate render target is big enough for the first mip.
			using (var renderTarget = new RenderTarget(_device.Device, mipWidth, mipHeight, texture.SRV.Description.Format))
			{
				// Use immediate context for drawing.
				var context = _device.Device.ImmediateContext;

				// Set common state.
				context.Rasterizer.State = _device.GlobalResources.RastStateCache.Get(RastState.Default.ToD3D11());
				context.OutputMerger.DepthStencilState = _device.GlobalResources.DepthStencilStateCache.Get(DepthStencilState.DisableDepth.ToD3D11());
				context.OutputMerger.BlendState = _device.GlobalResources.BlendStateCache.Get(BlendState.NoBlending.ToD3D11());

				// Set input layout
				context.InputAssembler.InputLayout = _device.GlobalResources.InputLayoutCache.GetInputLayout(
					_device.Device, _vertexShader.Signature, FullscreenQuad.InputElements);

				var texVariable = pixelShader.FindResourceVariable("tex");
				if (texVariable != null)
				{
					texVariable.Resource = texture.SRV;
					texVariable.Sampler = _device.GlobalResources.SamplerStateCache.Get(SamplerState.LinearClamp.ToD3D11());
					texVariable.SetToDevice(context);
				}

				int mip = 1;
				while (mipWidth > 0 && mipHeight > 0)
				{
					context.OutputMerger.SetTargets(renderTarget.RTV);
					context.Rasterizer.SetViewports(new SlimDX.Direct3D11.Viewport(0, 0, mipWidth, mipHeight));

					_vertexShader.Set(context);
					pixelShader.Set(context);

					// Render 'fullscreen' quad to downsample the mip.
					_device.GlobalResources.FullscreenQuad.Draw(context);

					// Copy result back to the mip chain of the source texture.
					context.CopySubresourceRegion(renderTarget.Texture2D, 0, texture.Texture2D, mip, 0, 0, 0);

					// Move to the next mip.
					mipWidth >>= 1;
					mipHeight >>= 1;
					mip++;
				}
			}
		}

		public void Dispose()
		{
		}
	}
}
