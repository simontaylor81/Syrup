using SlimDX;
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
		private Func<object> _getter;
		private Action<object> _setter;
		private FloatFieldProperty[] _fields;

		public int NumComponents { get { return _fields.Length; } }
		public string Name { get; private set; }
		public bool IsReadOnly { get { return false; } }

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return Observable.Merge(_fields).Subscribe(observer);
		}

		public IUserProperty GetComponent(int index)
		{
			return _fields[index];
		}

		// Can't pass a struct by reference, so pass getter and setter functions.
		public StructUserProperty(string name, Func<object> getter, Action<object> setter)
		{
			Name = name;
			_getter = getter;
			_setter = setter;

			_fields = getter().GetType().GetFields()
				.Where(f => f.FieldType == typeof(float))			// Only float fields supported currently.
				.Select(f => new FloatFieldProperty(getter, setter, f))
				.ToArray();
		}
	}

	internal class FloatFieldProperty : IScalarProperty<float>
	{
		private Func<object> _getter;
		private Action<object> _setter;
		private FieldInfo _field;
		private Subject<Unit> _subject = new Subject<Unit>();

		public FloatFieldProperty(Func<object> getter, Action<object> setter, FieldInfo field)
		{
			Debug.Assert(field.FieldType == typeof(float));
			_getter = getter;
			_setter = setter;
			_field = field;
		}

		public bool IsReadOnly { get { return false; } }

		public string Name { get { return _field.Name; } }

		public float Value
		{
			get
			{
				return (float)_field.GetValue(_getter());
            }
			set
			{
				if (value != Value)
				{
					var obj = _getter();
					_field.SetValue(obj, value);
					_setter(obj);
					_subject.OnNext(Unit.Default);
				}
			}
		}

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _subject.Subscribe(observer);
		}
	}
}
