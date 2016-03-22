using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SRPCommon.Scripting;
using SRPRendering.Resources;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	class ShaderUavVariable
	{
		public string Name { get; }

		public UnorderedAccessView UAV { get; set; }

		public void SetToDevice(DeviceContext context)
		{
			context.ComputeShader.SetUnorderedAccessView(_slot, UAV);
		}

		public void Reset()
		{
			UAV = null;
		}

		public ShaderUavVariable(InputBindingDescription desc, ShaderFrequency shaderFrequency)
		{
			if (shaderFrequency != ShaderFrequency.Compute)
			{
				throw new ScriptException("UAVs are only supported for compute shaders.");
			}

			Name = desc.Name;
			_slot = desc.BindPoint;

			// TODO: Support arrays.
			Trace.Assert(desc.BindCount == 1);
		}

		private int _slot;
	}
}
