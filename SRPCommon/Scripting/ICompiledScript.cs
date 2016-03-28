using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPScripting;

namespace SRPCommon.Scripting
{
	// A script that has been compiled and is ready to be executed.
	public interface ICompiledScript
	{
		Task ExecuteAsync(IRenderInterface renderInterface);
	}
}
