using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SRPScripting;

namespace SRPRendering.Resources
{
	interface ID3DDepthBuffer : ID3DShaderResource
	{
		DepthStencilView DSV { get; }
	}

	// For now, this class just contains special handles to the 'default' and 'null' depth buffers.
	// TODO: User-allocated depth buffers.
	static class DepthBufferHandle
	{
		public static DefaultDepthBuffer Default { get; } = new DefaultDepthBuffer();
		public static IDepthBuffer Null { get; } = new NullDepthBuffer();
	}

	// IDepthBuffer implementation for the default handle.
	class DefaultDepthBuffer : IDepthBuffer, ID3DDepthBuffer
	{
		// The actual depth buffer, updated each frame by the script render control.
		public DepthBuffer DepthBuffer { get; set; }

		public DepthStencilView DSV => DepthBuffer.DSV;
		public ShaderResourceView SRV => DepthBuffer.SRV;

		public UnorderedAccessView UAV { get { throw new NotImplementedException("Depth Buffer UAVs are currently unsupported."); } }

		// Nothing to dispose.
		public void Dispose() { }
	}

	// IDepthBuffer implementation for the null handle.
	class NullDepthBuffer : IDepthBuffer, ID3DDepthBuffer
	{
		public DepthStencilView DSV => null;

		public ShaderResourceView SRV { get { throw new NotImplementedException("Cannot bind null depth buffer to shaders."); } }
		public UnorderedAccessView UAV { get { throw new NotImplementedException("Cannot bind null depth buffer to shaders."); } }

		// Nothing to dispose.
		public void Dispose() { }
	}
}
