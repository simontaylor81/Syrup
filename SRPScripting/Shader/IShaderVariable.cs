using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting.Shader
{
	/// <summary>
	/// A single constant (non-resource) shader input.
	/// </summary>
	public interface IShaderVariable
	{
		/// <summary>
		/// Name of the variable.
		/// </summary>
		string Name { get; }
	}
}
