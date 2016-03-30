using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SRPCommon.Scripting;
using SRPScripting;

namespace SRPRendering.Resources
{
	// For now, this class just contains special handles to the 'default' and 'null' depth buffers.
	// TODO: User-allocated depth buffers.
	static class DepthBufferHandle
	{
		public static DefaultDepthBuffer Default { get; } = new DefaultDepthBuffer();
		public static IDepthBuffer Null { get; } = new NullDepthBuffer();
	}

	// IDepthBuffer implementation for the default handle.
	class DefaultDepthBuffer : IDepthBuffer, IViewDependentDepthBuffer
	{
		public ID3DShaderResource GetShaderResource(ViewInfo viewInfo) => viewInfo.DepthBuffer;
		public DepthStencilView GetDSV(ViewInfo viewInfo) => viewInfo.DepthBuffer.DSV;

	}

	// IDepthBuffer implementation for the null handle.
	class NullDepthBuffer : IDepthBuffer, IViewDependentDepthBuffer
	{
		public ID3DShaderResource GetShaderResource(ViewInfo viewInfo)
		{
			throw new ScriptException("Cannot bind null depth buffer to shaders.");
		}

		public DepthStencilView GetDSV(ViewInfo viewInfo) => null;
	}
}
