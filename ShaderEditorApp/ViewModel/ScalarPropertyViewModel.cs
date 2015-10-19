using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	// View-model class for scalar properties (i.e. properties with a single value).
	class ScalarPropertyViewModel<T> : PropertyViewModel
	{
		public ScalarPropertyViewModel(IScalarProperty<T> property)
			: base(property)
		{
			_property = property;
		}

		// The value of the property.
		public T Value
		{
			get { return _property.Value; }
			set { _property.Value = value; }
		}

		private readonly IScalarProperty<T> _property;
	}
}
