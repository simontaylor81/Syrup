using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	// Null implementations of the various shader constant variable types.
	// This allows use to always return something valid from Shader.Find*Variable,
	// meaning that the user doesn't have to worry about variables that have been compiled out.
	// All these classes just do nothing.

	class NullShaderConstantVariable : IShaderConstantVariable
	{
		public NullShaderConstantVariable(string name)
		{
			Name = name;
		}

		public bool IsNull => true;
		public string Name { get; }

		public void Bind(ShaderConstantVariableBindSource bindSource) { }
		public void BindToMaterial(string param) { }
		public void MarkAsScriptOverride() { }
		public void Set(dynamic value) { }
	}

	class NullShaderResourceVariable : IShaderResourceVariable
	{
		public NullShaderResourceVariable(string name)
		{
			Name = name;
		}

		public bool IsNull => true;
		public string Name { get; }

		public void BindToMaterial(string param, IShaderResource fallback = null) { }
		public void Set(IShaderResource resource) { }
	}

	class NullShaderSamplerVariable : IShaderSamplerVariable
	{
		public NullShaderSamplerVariable(string name)
		{
			Name = name;
		}

		public bool IsNull => true;
		public string Name { get; }

		public void Set(SamplerState state) { }
	}

	class NullShaderUavVariable : IShaderUavVariable
	{
		public NullShaderUavVariable(string name)
		{
			Name = name;
		}

		public bool IsNull => true;
		public string Name { get; }

		public void Set(IShaderResource resource) { }
	}
}
