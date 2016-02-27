using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	public class StructUserProperty : IVectorProperty
	{
		private readonly Func<object> _getter;
		private readonly Action<object> _setter;
		private readonly ObjectFieldUserProperty<float>[] _fields;

		public string Name { get; }
		public bool IsReadOnly => false;

		public int NumComponents => _fields.Length;
		public IUserProperty GetComponent(int index) => _fields[index];

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return Observable.Merge(_fields).Subscribe(observer);
		}

		// Can't pass a struct by reference, so pass getter and setter functions.
		public StructUserProperty(string name, Func<object> getter, Action<object> setter)
		{
			Name = name;
			_getter = getter;
			_setter = setter;

			_fields = getter().GetType().GetFields()
				.Where(f => f.FieldType == typeof(float))			// Only float fields supported currently.
				.Select(f => new ObjectFieldUserProperty<float>(f, getter, setter))
				.ToArray();
		}
	}

	// Base class for user properties that reflect over a class or struct member.
	public abstract class MemberProperty : IUserProperty
	{
		private MemberInfo _member;
		protected Subject<Unit> Subject { get; set; } = new Subject<Unit>();

		protected MemberProperty(MemberInfo member)
		{
			_member = member;
		}

		public bool IsReadOnly => false;

		public string Name => _member.Name;

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return Subject.Subscribe(observer);
		}
	}

	// User property for a class or struct field.
	public class ObjectFieldUserProperty<T> : MemberProperty, IScalarProperty<T>
	{
		private readonly Func<object> _getter;
		private readonly Action<object> _setter;
		private readonly FieldInfo _field;

		// Construct for a struct member, which can't be stored as a reference, so need explicit getter and setters.
		public ObjectFieldUserProperty(FieldInfo field, Func<object> objectGetter, Action<object> objectSetter)
			: base(field)
		{
			Debug.Assert(field.FieldType == typeof(T));

			_field = field;
			_getter = objectGetter;
			_setter = objectSetter;
		}

		// Convenience constructor for fields of a reference type, for which we can just store a object.
		public ObjectFieldUserProperty(FieldInfo field, object obj)
			: base(field)
		{
			Debug.Assert(field.FieldType == typeof(T));
			_field = field;

			_getter = () => obj;
			_setter = val => { };		// No need to set, since we're modifying the original object.
		}

		public Type Type => typeof(T);

		public T Value
		{
			get
			{
				return (T)_field.GetValue(_getter());
			}
			set
			{
				if (!value.Equals(Value))
				{
					var obj = _getter();
					_field.SetValue(obj, value);
					_setter(obj);
					Subject.OnNext(Unit.Default);
				}
			}
		}
	}

	// User property for a class or struct property (must have accessible get and set).
	public class ObjectPropertyUserProperty<T> : MemberProperty, IScalarProperty<T>
	{
		private readonly Func<object> _getter;
		private readonly Action<object> _setter;
		private readonly PropertyInfo _prop;

		// Construct for a struct member, which can't be stored as a reference, so need explicit getter and setters.
		public ObjectPropertyUserProperty(PropertyInfo prop, Func<object> objectGetter, Action<object> objectSetter)
			: base(prop)
		{
			Debug.Assert(prop.PropertyType == typeof(T));

			_prop = prop;
			_getter = objectGetter;
			_setter = objectSetter;
		}

		// Convenience constructor for fields of a reference type, for which we can just store a object.
		public ObjectPropertyUserProperty(PropertyInfo prop, object obj)
			: base(prop)
		{
			Debug.Assert(prop.PropertyType == typeof(T));
			_prop = prop;

			_getter = () => obj;
			_setter = val => { };		// No need to set, since we're modifying the original object.
		}

		public Type Type => typeof(T);

		public T Value
		{
			get
			{
				return (T)_prop.GetValue(_getter());
			}
			set
			{
				if (!value.Equals(Value))
				{
					var obj = _getter();
					_prop.SetValue(obj, value);
					_setter(obj);
					Subject.OnNext(Unit.Default);
				}
			}
		}
	}
}
