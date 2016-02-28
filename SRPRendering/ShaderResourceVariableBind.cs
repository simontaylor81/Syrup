using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	public interface IShaderResourceVariableBind
	{
		ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources);
	}

	class MaterialShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public MaterialShaderResourceVariableBind(string paramName, Texture fallback)
		{
			_paramName = paramName;
			_fallback = fallback;
		}

		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			// Look up texture filename in the material.
			if (primitive != null && primitive.Material != null)
			{
				string filename;
				if (primitive.Material.Textures.TryGetValue(_paramName, out filename))
				{
					// Get the actual texture object from the scene.
					return primitive.Scene.GetTexture(filename).SRV;
				}
			}

			// Fall back to fallback texture.
			return _fallback.SRV;
		}

		private readonly string _paramName;
		private readonly Texture _fallback;
	}

	class TextureShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public TextureShaderResourceVariableBind(Texture texture)
		{
			this.texture = texture;
		}

		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			return texture.SRV;
		}

		private readonly Texture texture;
	}

	class RenderTargetShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public RenderTargetShaderResourceVariableBind(RenderTargetDescriptor descriptor)
		{
			this.descriptor = descriptor;
		}

		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			System.Diagnostics.Debug.Assert(descriptor.renderTarget != null);
			return descriptor.renderTarget.SRV;
		}

		private readonly RenderTargetDescriptor descriptor;
	}

	class DefaultDepthBufferShaderResourceVariableBind : IShaderResourceVariableBind
	{
		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			return viewInfo.DepthBuffer.SRV;
		}
	}

	class BufferShaderResourceVariableBind : IShaderResourceVariableBind
	{
		private readonly Resources.Buffer _buffer;

		public BufferShaderResourceVariableBind(Resources.Buffer buffer)
		{
			_buffer = buffer;
		}

		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			return _buffer.SRV;
		}
	}
}
