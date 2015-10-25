using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	// Scalar property that the user can modify.
	public class MutableScalarProperty<T> : IScalarProperty<T>
	{
		public MutableScalarProperty(string name, T value)
		{
			Name = name;
			_value = value;
		}

		// IUserProperty interface
		public string Name { get; }
		public bool IsReadOnly => false;

		public Type Type => typeof(T);

		// IScalarProperty interface
		public T Value
		{
			get { return _value; }
			set
			{
				_value = value;
				_subject.OnNext(Unit.Default);
			}
		}

		// IObservable interface.
		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _subject.Subscribe(observer);
		}

		private T _value;
		private Subject<Unit> _subject = new Subject<Unit>();
	}
}
