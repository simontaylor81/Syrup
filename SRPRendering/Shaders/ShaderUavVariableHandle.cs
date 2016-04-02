using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SRPCommon.Scripting;
using SRPRendering.Resources;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	class ShaderUavVariableHandle : IShaderUavVariable
	{
		// IShaderVariable interface
		public string Name { get; }

		public UavHandle UAV { get; private set; }

		// IShaderUavVariable interface
		public void Set(IUav iuav)
		{
			if (UAV != null)
			{
				throw new ScriptException("Attempting to set already set UAV variable: " + Name);
			}

			var handle = iuav as UavHandle;
			if (iuav != null && handle == null)
			{
				throw new ScriptException("Invalid UAV");
			}

			UAV = handle;
		}

		public ShaderUavVariableHandle(string name)
		{
			Name = name;
		}
	}
}
