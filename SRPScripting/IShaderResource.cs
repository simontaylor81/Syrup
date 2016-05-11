using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting
{
	// A resource that can be bound to a shader (texture or buffer, basically).
	public interface IShaderResource
	{
	}

	// A 1D buffer.
	public interface IBuffer : IShaderResource
	{
		// Create a UAV for this resource.
		// TODO: Move to base interface and implement for textures.
		IUav CreateUav();

		// Mark this buffer as containing indirect draw arguments.
		IBuffer ContainsDrawIndirectArgs();
	}

	// A 2D texture.
	public interface ITexture2D : IShaderResource
	{
		// Sets whether or not to generate mipmaps for this texture.
		// Returns the same object for convenience.
		ITexture2D WithMips(bool generateMips);

		// Generate custom mipmaps for this texture using the provided shader file.
		// Returns the same object for convenience.
		ITexture2D WithCustomMips(string shaderFilename);
	}

	// A render target.
	public interface IRenderTarget : IShaderResource
	{
	}

	// A depth buffer.
	public interface IDepthBuffer : IShaderResource
	{
	}

	// A UAV
	public interface IUav
	{
	}
}
