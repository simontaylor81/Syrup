using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	// Info about a sampler input to a shader.
	public interface IShaderSamplerVariable
	{
		/// <summary>
		/// Name of the sampler variable.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The sampler state to set.
		/// </summary>
		SamplerState State { get; set; }

		/// <summary>
		/// Value binding.
		/// </summary>
		IShaderSamplerVariableBind Bind { get; set; }
	}

	class ShaderSamplerVariable : IShaderSamplerVariable
	{
		public string Name { get; }
		public SamplerState State { get; set; }
		public IShaderSamplerVariableBind Bind { get; set; }

		private readonly int _slot;
		private readonly ShaderFrequency _shaderFrequency;

		public ShaderSamplerVariable(InputBindingDescription desc, ShaderFrequency frequency)
		{
			Name = desc.Name;
			_slot = desc.BindPoint;
			_shaderFrequency = frequency;
		}

		public void SetToDevice(DeviceContext context)
		{
			switch (_shaderFrequency)
			{
				case ShaderFrequency.Vertex:
					context.VertexShader.SetSampler(_slot, State);
					break;

				case ShaderFrequency.Pixel:
					context.PixelShader.SetSampler(_slot, State);
					break;

				case ShaderFrequency.Compute:
					context.ComputeShader.SetSampler(_slot, State);
					break;
			}
		}
	}
}
