using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SRPRendering.Resources;

namespace SRPRendering.Shaders
{
	interface IShaderResourceVariableBinding
	{
		ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources);
	}

	// Bind a shader resource variable to a material parameter.
	class MaterialShaderResourceVariableBinding : IShaderResourceVariableBinding
	{
		public MaterialShaderResourceVariableBinding(string paramName, IDeferredResource fallback)
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
			var fallback = _fallback?.Resource ?? globalResources.ErrorTexture;
			return fallback.SRV;
		}

		private readonly string _paramName;
		private readonly IDeferredResource _fallback;
	}

	// Bind a shader resource variable to a deferred resource (i.e. one created by script).
	class DeferredResourceShaderResourceVariableBinding : IShaderResourceVariableBinding
	{
		public DeferredResourceShaderResourceVariableBinding(IDeferredResource resource)
		{
			_resource = resource;
		}

		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			return _resource.Resource.SRV;
		}

		private readonly IDeferredResource _resource;
	}

	// Bind a shader resource variable to a render target.
	// Not really anything special currently, but will get more complicated if we add multiple viewports.
	class ViewDependentShaderResourceVariableBinding : IShaderResourceVariableBinding
	{
		private readonly IViewDependentResource _renderTarget;

		public ViewDependentShaderResourceVariableBinding(IViewDependentResource renderTarget)
		{
			_renderTarget = renderTarget;
		}

		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			return _renderTarget.GetShaderResource(viewInfo).SRV;
		}
	}

	// Bind a shader resource variable directly to a concrete resource.
	// Only used when driving shaders directly from code (e.g. MipGenerator).
	class DirectShaderResourceVariableBinding : IShaderResourceVariableBinding
	{
		public DirectShaderResourceVariableBinding(ID3DShaderResource resource)
		{
			_resource = resource;
		}

		public ShaderResourceView GetResource(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			return _resource.SRV;
		}

		private readonly ID3DShaderResource _resource;
	}
}
