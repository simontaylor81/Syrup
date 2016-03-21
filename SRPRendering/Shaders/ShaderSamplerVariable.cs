using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SRPCommon.Scripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	// Info about a sampler input to a shader.
	class ShaderSamplerVariable : IShaderSamplerVariable
	{
		// IShaderVariable interface.
		public string Name { get; }

		// IShaderSamplerVariable interface.
		public void Set(SRPScripting.SamplerState state)
		{
			if (_state != null)
			{
				throw new ScriptException("Attempting to set already set sampler variable: " + Name);
			}

			_state = state;
		}

		private readonly int _slot;
		private readonly ShaderFrequency _shaderFrequency;
		private SRPScripting.SamplerState _state;

		public ShaderSamplerVariable(InputBindingDescription desc, ShaderFrequency frequency)
		{
			Name = desc.Name;
			_slot = desc.BindPoint;
			_shaderFrequency = frequency;
		}

		public void SetToDevice(DeviceContext context, IGlobalResources globalResources)
		{
			var d3dState = globalResources.SamplerStateCache.Get(_state.ToD3D11());

			switch (_shaderFrequency)
			{
				case ShaderFrequency.Vertex:
					context.VertexShader.SetSampler(_slot, d3dState);
					break;

				case ShaderFrequency.Pixel:
					context.PixelShader.SetSampler(_slot, d3dState);
					break;

				case ShaderFrequency.Compute:
					context.ComputeShader.SetSampler(_slot, d3dState);
					break;
			}
		}

		public void Reset()
		{
			_state = null;
		}
	}
}
