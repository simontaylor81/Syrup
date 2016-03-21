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
		int ElementCount { get; }
		int SizeInBytes { get; }

		IEnumerable<T> GetContents<T>() where T : struct;
	}

	// A 2D texture.
	public interface ITexture2D : IShaderResource
	{
		int Width { get; }
		int Height { get; }
	}
}
