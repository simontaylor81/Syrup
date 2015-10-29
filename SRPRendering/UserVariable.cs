﻿using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;
using System.Linq;
using System.Reactive.Linq;

namespace SRPRendering
{
	abstract class UserVariable<TValue> : IUserProperty
	{
		// IUserProperty interface
		public string Name { get; }
		public bool IsReadOnly => false;

		public abstract Func<TValue> GetFunction();

		// IObservable interface
		public abstract IDisposable Subscribe(IObserver<Unit> observer);

		protected UserVariable(string name)
		{
			this.Name = name;
		}
	}

	static class UserVariable
	{
		public static UserVariable<T> CreateScalar<T>(string name, T defaultValue)
		{
			return new UserVariableScalar<T>(name, defaultValue);
		}

		public static UserVariable<T[]> CreateVector<T>(int numComponents, string name, object defaultValue)
		{
			dynamic dynamicDefault = defaultValue;
			try
			{
				var components = Enumerable.Range(0, numComponents)
					.Select(i => new UserVariableScalar<T>(i.ToString(), dynamicDefault[i]))
					.ToArray();
				return new UserVariableVector<T>(name, components);
			}
			catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
			{
				throw new ScriptException($"Incorrect type for user variable '{name}' default value.", ex);
			}
		}
	}

	// User variable representing a single value of the given type.
	class UserVariableScalar<T> : UserVariable<T>, IScalarProperty<T>
	{
		public override Func<T> GetFunction() => (() => value);

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
	class UserVariableVector<TComponent> : UserVariable<TComponent[]>, IVectorProperty
	{
		private readonly UserVariable<TComponent>[] components;

		public override Func<TComponent[]> GetFunction()
		{
			// Get the function for each component.
			var subFuncs = components.Select(c => c.GetFunction());

			// Convert result to array so it's subscriptable in script.
			return () => subFuncs.Select(s => s()).ToArray();
		}

		public UserVariableVector(string name, UserVariable<TComponent>[] components)
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
