using System;
using System.Reactive;
using System.Reactive.Disposables;

namespace SRPCommon.UserProperties
{
	// Simple implementation of IScalarProperty which stores a single read-only value.
	public class ReadOnlyScalarProperty<T> : IScalarProperty<T>
	{
		public ReadOnlyScalarProperty(string name, T value)
		{
			Name = name;
			_value = value;
		}

		// IUserProperty interface
		public string Name { get; }
		public bool IsReadOnly => true;

		public Type Type => typeof(T);

		// IScalarProperty interface
		public T Value
		{
			get { return _value; }
			set { throw new InvalidOperationException("Cannot set a read-only property"); }
		}

		// IObservable interface.
		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			// Read-only so never changes.
			return Disposable.Empty;
		}

		private T _value;
	}
}
