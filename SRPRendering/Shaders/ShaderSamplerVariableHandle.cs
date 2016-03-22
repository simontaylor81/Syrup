using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scripting;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	class ShaderSamplerVariableHandle : IShaderSamplerVariable
	{
		public string Name { get; }

		public SamplerState State { get; private set; }

		// IShaderSamplerVariable interface.
		public void Set(SRPScripting.SamplerState state)
		{
			if (State != null)
			{
				throw new ScriptException("Attempting to set already set sampler variable: " + Name);
			}

			State = state;
		}

		public ShaderSamplerVariableHandle(string name)
		{
			Name = name;
		}
	}
}
