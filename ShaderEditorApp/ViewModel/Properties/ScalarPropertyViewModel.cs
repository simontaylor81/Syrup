using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel.Properties
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

	// Factory for choice property view models.
	class ScalarPropertyViewModelFactory : IPropertyViewModelFactory
	{
		public int Priority => 20;

		public bool SupportsProperty(IUserProperty property) => property is IScalarProperty;

		public PropertyViewModel CreateInstance(IUserProperty property)
		{
			var scalarProperty = (IScalarProperty)property;

			// Construct the type of the view model with the same type as the property.
			var viewModelType = typeof(ScalarPropertyViewModel<object>).GetGenericTypeDefinition().MakeGenericType(scalarProperty.Type);

			// Create an instance of the view model type.
			return (PropertyViewModel)Activator.CreateInstance(viewModelType, property);
		}
	}

}
