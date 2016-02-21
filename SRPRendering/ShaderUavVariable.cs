using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SRPCommon.Scripting;

namespace SRPRendering
{
	public interface IShaderUavVariable
	{
		/// <summary>
		/// Name of the variable.
		/// </summary>
		string Name { get; }

		UnorderedAccessView UAV { get; set; }

		/// <summary>
		/// Submit the current value to the graphics device.
		/// </summary>
		void SetToDevice(DeviceContext context);
	}

	class ShaderUavVariable : IShaderUavVariable
	{
		public string Name { get; }

		public UnorderedAccessView UAV { get; set; }

		public void SetToDevice(DeviceContext context)
		{
			if (_shaderFrequency != ShaderFrequency.Compute)
			{
				throw new ScriptException("UAVs are only supported for compute shaders.");
			}

			context.ComputeShader.SetUnorderedAccessView(UAV, _slot);
		}

		public ShaderUavVariable(InputBindingDescription desc, ShaderFrequency shaderFrequency)
		{
			Name = desc.Name;
			_slot = desc.BindPoint;

			_shaderFrequency = shaderFrequency;

			// TODO: Support arrays.
			System.Diagnostics.Debug.Assert(desc.BindCount == 1);
		}

		private int _slot;
		private ShaderFrequency _shaderFrequency;
	}
}
