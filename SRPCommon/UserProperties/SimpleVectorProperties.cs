using SlimDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SRPCommon.UserProperties
{
	/*
	public class Vector3Property : IVectorProperty<float>
	{
		private Vector3 _value;
		private Subject<Unit> _subject = new Subject<Unit>();

		public float this[int index]
		{
			get { return _value[index]; }
			set
			{
				if (_value[index] != value)
				{
					_value[index] = value;
					_subject.OnNext(Unit.Default);
				}
			}
		}

		public int NumComponents { get { return 3; } }
		public string Name { get; private set; }
		public bool IsReadOnly { get { return false; } }

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _subject.Subscribe(observer);
		}

		public Vector3Property(string name, Vector3 value)
		{
			Name = name;
			_value = value;
		}
	}
	*/
}
