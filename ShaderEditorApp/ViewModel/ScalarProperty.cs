using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	// Generic abstract base class for scalar properties (i.e. properties with a single value).
	abstract class ScalarPropertyBase<T> : PropertyViewModel
	{
		// The value of the property.
		public abstract T Value { get; set; }

		public ScalarPropertyBase(string name)
			: base(name)
		{
		}
	}

	// A simple implementation of a scalar property that stores its own value,
	// and can optionally notify the user when the value changes.
	class SimpleScalarProperty<T> : ScalarPropertyBase<T>
	{
		// Constructor without notification, optionally read-only.
		public SimpleScalarProperty(string name, T initialValue, bool readOnly)
			: base(name)
		{
			value_ = initialValue;
			IsReadOnly = readOnly;
		}

		// Constructor with change notification callback.
		public SimpleScalarProperty(string name, T initialValue, Action<T> changedAction)
			: base(name)
		{
			value_ = initialValue;
			PropertyChanged += (o, e) => changedAction(Value);
		}

		private T value_;
		public override T Value
		{
			get { return value_; }
			set
			{
				if (!value.Equals(value_))
				{
					value_ = value;
					OnPropertyChanged();
				}
			}
		}
	}
}
