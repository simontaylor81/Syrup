using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting.Shader
{
	// Shader resource (texture, buffer, etc.) variable.
	public interface IShaderResourceVariable : IShaderVariable
	{
		// Set directly to a given resource.
		void Set(IShaderResource resource);

		// Bind to a material property.
		void BindToMaterial(string param, IShaderResource fallback = null);

		// Is this a null implementation for a missing variable?
		bool IsNull { get; }
	}
}
