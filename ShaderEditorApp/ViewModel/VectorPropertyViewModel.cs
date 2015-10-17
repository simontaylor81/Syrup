using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel
{
	// View-model class for vector properties. I.e. those with multiple values of a particular type.
	class VectorPropertyViewModel : PropertyViewModel
	{
		public VectorPropertyViewModel(IVectorProperty property)
			: base(property)
		{
			SubProperties = Enumerable.Range(0, property.NumComponents)
				.Select(i => PropertyViewModelFactory.CreateViewModel(property.GetComponent(i)))
				.ToArray();
		}

		public PropertyViewModel[] SubProperties { get; }
	}
}
