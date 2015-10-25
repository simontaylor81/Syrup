using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public interface IScalarProperty : IUserProperty
	{
		// Type of the property.
		Type Type { get; }
	}

	public interface IScalarProperty<T> : IScalarProperty
	{
		// The value of the property.
		T Value { get; set; }
	}
}
