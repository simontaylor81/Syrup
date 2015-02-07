using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public interface IVectorProperty : IUserProperty
	{
		// The value of a component in the vector.
		IUserProperty GetComponent(int index);

		// The number of components in the vector.
		int NumComponents { get; }
	}
}
