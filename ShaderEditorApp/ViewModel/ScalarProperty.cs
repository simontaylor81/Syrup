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
}
