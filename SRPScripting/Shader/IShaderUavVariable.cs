using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting.Shader
{
	public interface IShaderUavVariable : IShaderVariable
	{
		void Set(IShaderResource resource);

		// Is this a null implementation for a missing variable?
		bool IsNull { get; }
	}
}
