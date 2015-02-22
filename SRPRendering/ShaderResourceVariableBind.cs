using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D11;

namespace SRPRendering
{
	public interface IShaderResourceVariableBind
	{
		void Set(IPrimitive primitive, ViewInfo viewInfo, IShaderResourceVariable variable, IGlobalResources globalResources);
	}

	class MaterialShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public MaterialShaderResourceVariableBind(string paramName)
		{
			this.paramName = paramName;
		}

		public void Set(IPrimitive primitive, ViewInfo viewInfo, IShaderResourceVariable variable, IGlobalResources globalResources)
		{
			// TODO: user set sampler state somehow.
			variable.Sampler = globalResources.SamplerStateCache.Get(SRPScripting.SamplerState.LinearWrap.ToD3D11());

			// Look up texture filename in the material.
			if (primitive != null && primitive.Material != null)
			{
				string filename;
				if (primitive.Material.Textures.TryGetValue(paramName, out filename))
				{
					// Get the actual texture object from the scene.
					variable.Resource = primitive.Scene.GetTexture(filename).SRV;
					return;
				}
			}

			// Fall back on error texture.
			variable.Resource = globalResources.ErrorTexture.SRV;
		}

		private string paramName;
	}

	class TextureShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public TextureShaderResourceVariableBind(Texture texture)
		{
			this.texture = texture;
		}

		public void Set(IPrimitive primitive, ViewInfo viewInfo, IShaderResourceVariable variable, IGlobalResources globalResources)
		{
			// TODO: user set sampler state somehow.
			variable.Sampler = globalResources.SamplerStateCache.Get(SRPScripting.SamplerState.PointClamp.ToD3D11());
			variable.Resource = texture.SRV;
		}

		private Texture texture;
	}

	class RenderTargetShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public RenderTargetShaderResourceVariableBind(RenderTargetDescriptor descriptor)
		{
			this.descriptor = descriptor;
		}

		public void Set(IPrimitive primitive, ViewInfo viewInfo, IShaderResourceVariable variable, IGlobalResources globalResources)
		{
			// TODO: user set sampler state somehow.
			variable.Sampler = globalResources.SamplerStateCache.Get(SRPScripting.SamplerState.PointClamp.ToD3D11());

			System.Diagnostics.Debug.Assert(descriptor.renderTarget != null);
			variable.Resource = descriptor.renderTarget.SRV;
		}

		private RenderTargetDescriptor descriptor;
	}

	class DefaultDepthBufferShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public void Set(IPrimitive primitive, ViewInfo viewInfo, IShaderResourceVariable variable, IGlobalResources globalResources)
		{
			// TODO: user set sampler state somehow.
			variable.Sampler = globalResources.SamplerStateCache.Get(SRPScripting.SamplerState.PointClamp.ToD3D11());

			variable.Resource = viewInfo.DepthBuffer.SRV;
		}
	}
}
