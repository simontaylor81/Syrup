using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting
{
	// A handle to a D3D Buffer
	public interface IBuffer
	{
		IEnumerable<T> GetContents<T>() where T : struct;
	}
}
