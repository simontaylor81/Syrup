using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public interface IVectorProperty<T> : IUserProperty
	{
		// The value of a component in the vector.
		T this[int index] { get; set; }

		// The number of components in the vector.
		int NumComponents { get; }
	}
}
