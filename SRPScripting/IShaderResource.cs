﻿using System;
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
	}

	// A 2D texture.
	public interface ITexture2D : IShaderResource
	{
	}

	// A render target.
	public interface IRenderTarget : IShaderResource
	{
	}

	// A depth buffer.
	public interface IDepthBuffer : IShaderResource
	{
	}
}
