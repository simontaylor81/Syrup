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

		public abstract object GetFunction();

		// IObservable interface
		public abstract IDisposable Subscribe(IObserver<Unit> observer);

		public static UserVariable CreateScalar<T>(string name, T defaultValue)
		{
			return new UserVariableScalar<T>(name, defaultValue);
		}

		// TODO: Typed default?
		public static UserVariable CreateVector<T>(int numComponents, string name, dynamic defaultValue)
		{
			try
			{
				var components = Enumerable.Range(0, numComponents)
					.Select(i => new UserVariableScalar<T>(i.ToString(), defaultValue[i]))
					.ToArray();
				return new UserVariableVector(name, components);
			}
			catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
			{
				throw new ScriptException($"Incorrect type for user variable '{name}' default value.", ex);
			}
		}

		protected UserVariable(string name)
		{
			this.Name = name;
		}
	}

	// User variable representing a single value of the given type.
	class UserVariableScalar<T> : UserVariable, IScalarProperty<T>
	{
		public override object GetFunction()
		{
			Func<T> func = () => value;
			return func;
		}

		public UserVariableScalar(string name, T defaultValue)
			: base(name)
		{
			value = defaultValue;
		}

		public Type Type => typeof(T);

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
		private readonly UserVariable[] components;

		public override dynamic GetFunction()
		{
			// Get the function for each component.
			IEnumerable<dynamic> subFuncs = components.Select(c => c.GetFunction());

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
