using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace SRPRendering.Resources
{
	// Interface for a resource that is (potentially) view dependent.
	// I.e. depth buffers and render targets.
	interface IViewDependentResource
	{
		ID3DShaderResource GetShaderResource(ViewInfo viewInfo);
	}

	interface IViewDependentRenderTarget : IViewDependentResource
	{
		RenderTarget GetRenderTarget(ViewInfo viewInfo);
	}

	interface IViewDependentDepthBuffer : IViewDependentResource
	{
		DepthStencilView GetDSV(ViewInfo viewInfo);
	}
}
