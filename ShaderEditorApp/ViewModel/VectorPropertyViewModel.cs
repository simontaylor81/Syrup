using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	// View-model class for vector properties. I.e. those with multiple values of a particular type.
	class VectorPropertyViewModel<T> : PropertyViewModel
	{
		public VectorPropertyViewModel(IVectorProperty<T> property)
			: base(property)
		{
			_property = property;
		}

		public T this[int index]
		{
			get { return _property[index]; }
			set { _property[index] = value; }
		}

		public int NumComponents { get { return _property.NumComponents; } }

		private IVectorProperty<T> _property;
	}
}
