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
	class ShaderSamplerVariable
	{
		public string Name { get; }

		public SRPScripting.SamplerState State { get; set; }

		private readonly int _slot;
		private readonly ShaderFrequency _shaderFrequency;

		public ShaderSamplerVariable(InputBindingDescription desc, ShaderFrequency frequency)
		{
			Name = desc.Name;
			_slot = desc.BindPoint;
			_shaderFrequency = frequency;
		}

		public void SetToDevice(DeviceContext context, IGlobalResources globalResources)
		{
			var d3dState = globalResources.SamplerStateCache.Get(State.ToD3D11());

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
			State = null;
		}
	}
}
