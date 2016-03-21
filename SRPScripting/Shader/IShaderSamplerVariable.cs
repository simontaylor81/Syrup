using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting.Shader
{
	public interface IShaderSamplerVariable : IShaderVariable
	{
		void Set(SamplerState state);

		// Is this a null implementation for a missing variable?
		bool IsNull { get; }
	}
}
