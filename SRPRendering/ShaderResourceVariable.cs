using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace SRPRendering
{
	public interface IShaderResourceVariable
	{
		/// <summary>
		/// Name of the variable.
		/// </summary>
		string Name { get; }

		ShaderResourceView Resource { get; set; }

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

		public IShaderResourceVariableBind Bind { get; set; }

		public void SetToDevice(DeviceContext context)
		{
			switch (shaderFrequency)
			{
				case ShaderFrequency.Vertex:
					context.VertexShader.SetShaderResource(slot, Resource);
					break;

				case ShaderFrequency.Pixel:
					context.PixelShader.SetShaderResource(slot, Resource);
					break;

				case ShaderFrequency.Compute:
					context.ComputeShader.SetShaderResource(slot, Resource);
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
			Trace.Assert(desc.BindCount == 1);
		}

		private int slot;
		private ShaderFrequency shaderFrequency;
	}
}
