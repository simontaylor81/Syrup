using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	// Interface for binding shader sampler inputs.
	public interface IShaderSamplerVariableBind
	{
		SamplerState GetState(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources);
	}

	// Class for binding the sampler input directly to a SamplerState descriptor, using the state cache.
	class ShaderSamplerVariableBindDirect : IShaderSamplerVariableBind
	{
		private readonly SRPScripting.SamplerState _state;

		public ShaderSamplerVariableBindDirect(SRPScripting.SamplerState state)
		{
			_state = state;
		}

		public SamplerState GetState(IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			return globalResources.SamplerStateCache.Get(_state.ToD3D11());
		}
	}
}
