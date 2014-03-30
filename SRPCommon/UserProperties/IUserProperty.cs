using System;
using System.Reactive;

namespace SRPCommon.UserProperties
{
	// A name-value pair that is exposed to the user.
	// IObseravable will fire when the value changes. Doesn't supply the value
	// because there is no single value for vector parameters.
	public interface IUserProperty : IObservable<Unit>
	{
		string Name { get; }
		bool IsReadOnly { get; }
	}
}
