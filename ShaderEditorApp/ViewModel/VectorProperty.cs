using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	// Generic base class for vector properties. I.e. those with multiple values of a particular type.
	abstract class VectorPropertyBase<T> : PropertyViewModel
	{
		public VectorPropertyBase(string name, int numComponents)
			: base(name)
		{
			this.NumComponents = numComponents;
		}

		public abstract T this[int index] { get; set; }

		public int NumComponents { get; private set; }
	}
}
