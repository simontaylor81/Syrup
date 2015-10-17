using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;

namespace SRPRendering
{
	public interface IShaderResourceVariable
	{
		/// <summary>
		/// Name of the variable.
		/// </summary>
		string Name { get; }

		ShaderResourceView Resource { get; set; }
		SamplerState Sampler { get; set; }

		IShaderResourceVariableBind Bind { get; set; }

		/// <summary>
		/// Submit the current value to the graphics device.
		/// </summary>
		void SetToDevice(DeviceContext context);
	}

	class ShaderResourceVariable : IShaderResourceVariable
	{
		// IShaderVariable interface.
		public string Name { get; }
		public ShaderResourceView Resource { get; set; }
		public SamplerState Sampler { get; set; }

		public IShaderResourceVariableBind Bind { get; set; }

		public void SetToDevice(DeviceContext context)
		{
			switch (shaderFrequency)
			{
				case ShaderFrequency.Vertex:
					context.VertexShader.SetShaderResource(Resource, slot);
					context.VertexShader.SetSampler(Sampler, slot);
					break;

				case ShaderFrequency.Pixel:
					context.PixelShader.SetShaderResource(Resource, slot);
					context.PixelShader.SetSampler(Sampler, slot);
					break;

				case ShaderFrequency.Compute:
					context.ComputeShader.SetShaderResource(Resource, slot);
					context.ComputeShader.SetSampler(Sampler, slot);
					break;
			}
		}

		// Constructors.
		public ShaderResourceVariable(InputBindingDescription desc, ShaderFrequency shaderFrequency)
		{
			Name = desc.Name;
			slot = desc.BindPoint;

			this.shaderFrequency = shaderFrequency;

			// TODO: Support arrays.
			System.Diagnostics.Debug.Assert(desc.BindCount == 1);
		}

		private int slot;
		private ShaderFrequency shaderFrequency;
	}
}
