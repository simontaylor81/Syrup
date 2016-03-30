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

		public IDeferredResource UAV { get; private set; }

		// IShaderUavVariable interface
		public void Set(IShaderResource iresource)
		{
			if (UAV != null)
			{
				throw new ScriptException("Attempting to set already set UAV variable: " + Name);
			}

			var resource = iresource as IDeferredResource;
			if (iresource != null && resource == null)
			{
				throw new ScriptException("Invalid shader resource");
			}

			// TODO: Some way to check that the resource has UAV enabled?

			UAV = resource;
		}

		public ShaderUavVariableHandle(string name)
		{
			Name = name;
		}
	}
}
