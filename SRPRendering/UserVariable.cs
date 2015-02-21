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
		public string Name { get; private set; }
		public bool IsReadOnly { get { return false; } }

		public abstract dynamic GetFunction();

		// IObservable interface
		public abstract IDisposable Subscribe(IObserver<Unit> observer);

		public static UserVariable Create(string name, UserVariableType type, dynamic defaultValue)
		{
			try
			{
				switch (type)
				{
					// Float scalar & vectors.
					case UserVariableType.Float:
						return new UserVariableFloat(name, defaultValue);
					case UserVariableType.Float2:
						return CreateVectorFloat(2, name, defaultValue);
					case UserVariableType.Float3:
						return CreateVectorFloat(3, name, defaultValue);
					case UserVariableType.Float4:
						return CreateVectorFloat(4, name, defaultValue);

					case UserVariableType.Bool:
						return new UserVariableBool(name, defaultValue);

					default:
						throw new ArgumentException("Invalid user variable type.");
				}
			}
			catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
			{
				throw new ScriptException("Incorrect type for user variable default value.", ex);
			}
		}

		private static UserVariable CreateVectorFloat(int numComponents, string name, dynamic defaultValue)
		{
			var components = Enumerable.Range(0, numComponents)
				.Select(i => new UserVariableFloat(i.ToString(), defaultValue[i]))
				.ToArray();
			return new UserVariableVector(name, components);
		}

		protected UserVariable(string name)
		{
			this.Name = name;
		}
	}

	// User variable representing a floating point vector or scalar with a variable number of components.
	class UserVariableFloat : UserVariable, IScalarProperty<float>
	{
		public override dynamic GetFunction()
		{
			Func<float> func = () => value;
			return func;
		}

		public UserVariableFloat(string name, dynamic defaultValue)
			: base(name)
		{
			// Use explicit cast to convert from ints/doubles.
			value = (float)defaultValue;
		}

		public float Value
		{
			get { return value; }
			set
			{
				if (this.value != value)
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
		private float value;

		private Subject<Unit> _subject = new Subject<Unit>();
	}

	class UserVariableBool : UserVariable, IScalarProperty<bool>
	{
		public override dynamic GetFunction()
		{
			Func<bool> func = () => Value;
			return func;
		}

		public UserVariableBool(string name, bool defaultValue)
			: base(name)
		{
			Value = defaultValue;
		}

		public bool Value
		{
			get { return _value; }
			set
			{
				if (_value != value)
				{
					_value = value;
					_subject.OnNext(Unit.Default);
				}
			}
		}

		// IObservable interface
		public override IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _subject.Subscribe(observer);
		}

		private bool _value;
		private Subject<Unit> _subject = new Subject<Unit>();
	}

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

		public IUserProperty GetComponent(int index)
		{
			return components[index];
		}
		public int NumComponents { get { return components.Length; } }

		public override IDisposable Subscribe(IObserver<Unit> observer)
		{
			return Observable.Merge(components).Subscribe(observer);
		}
	}
}
