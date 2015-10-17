using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using SRPScripting;
using System.Linq;
using System.Reactive.Linq;

namespace SRPRendering
{
	abstract class UserVariable : IUserProperty
	{
		// IUserProperty interface
		public string Name { get; }
		public bool IsReadOnly => false;

		public abstract dynamic GetFunction();

		// IObservable interface
		public abstract IDisposable Subscribe(IObserver<Unit> observer);

		public static UserVariable Create(string name, UserVariableType type, dynamic defaultValue)
		{
			try
			{
				switch (type)
				{
					// Scalar types.
					case UserVariableType.Float:
						return new UserVariableScalar<float>(name, defaultValue);
					case UserVariableType.Int:
						return new UserVariableScalar<int>(name, defaultValue);
					case UserVariableType.Bool:
						return new UserVariableScalar<bool>(name, defaultValue);
					case UserVariableType.String:
						return new UserVariableScalar<string>(name, defaultValue);

					// Vector types
					case UserVariableType.Float2:
						return CreateVector<float>(2, name, defaultValue);
					case UserVariableType.Float3:
						return CreateVector<float>(3, name, defaultValue);
					case UserVariableType.Float4:
						return CreateVector<float>(4, name, defaultValue);

					case UserVariableType.Int2:
						return CreateVector<int>(2, name, defaultValue);
					case UserVariableType.Int3:
						return CreateVector<int>(3, name, defaultValue);
					case UserVariableType.Int4:
						return CreateVector<int>(4, name, defaultValue);

					default:
						throw new ArgumentException("Invalid user variable type.");
				}
			}
			catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
			{
				throw new ScriptException("Incorrect type for user variable default value.", ex);
			}
		}

		private static UserVariable CreateVector<T>(int numComponents, string name, dynamic defaultValue)
		{
			var components = Enumerable.Range(0, numComponents)
				.Select(i => new UserVariableScalar<T>(i.ToString(), defaultValue[i]))
				.ToArray();
			return new UserVariableVector(name, components);
		}

		protected UserVariable(string name)
		{
			this.Name = name;
		}
	}

	// User variable representing a single value of the given type.
	class UserVariableScalar<T> : UserVariable, IScalarProperty<T>
	{
		public override dynamic GetFunction()
		{
			Func<T> func = () => value;
			return func;
		}

		public UserVariableScalar(string name, dynamic defaultValue)
			: base(name)
		{
			// Use explicit cast to convert from similar types (e.g. ints/doubles -> float).
			value = (T)defaultValue;
		}

		public T Value
		{
			get { return value; }
			set
			{
				if (!EqualityComparer<T>.Default.Equals(this.value, value))
				{
					this.value = value;
					_subject.OnNext(Unit.Default);
				}
			}
		}

		// IObservable interface
		public override IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _subject.Subscribe(observer);
		}

		// The storage of the actual value.
		private T value;

		private Subject<Unit> _subject = new Subject<Unit>();
	}

	// User variable representing a vector of values.
	class UserVariableVector : UserVariable, IVectorProperty
	{
		private UserVariable[] components;

		public override dynamic GetFunction()
		{
			// Get the function for each component.
			var subFuncs = components.Select(c => c.GetFunction());

			// Convert result to array so it's subscriptable in script.
			Func<IEnumerable<dynamic>> func = () => subFuncs.Select(s => s()).ToArray();
			return func;
		}

		public UserVariableVector(string name, UserVariable[] components)
			: base(name)
		{
			this.components = components;
		}

		public int NumComponents => components.Length;
		public IUserProperty GetComponent(int index) => components[index];

		public override IDisposable Subscribe(IObserver<Unit> observer)
		{
			return Observable.Merge(components).Subscribe(observer);
		}
	}
}
