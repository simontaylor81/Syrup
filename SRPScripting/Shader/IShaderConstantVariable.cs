using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting.Shader
{
	// A shader variable containing raw constant data (float/int) that goes in a constant buffer.
	public interface IShaderConstantVariable : IShaderVariable
	{
		// Set directly to a given value.
		void Set(dynamic value);

		// Bind to camera/scene property.
		void Bind(ShaderConstantVariableBindSource bindSource);

		// Bind to a material property.
		void BindToMaterial(string param);

		// Mark the variable as being overridden directly from script during rendering.
		void MarkAsScriptOverride();

		// Is this a null implementation for a missing variable?
		bool IsNull { get; }
	}
}
