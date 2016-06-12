using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.ViewModel.Properties
{
	class Vec3PropertyViewModel : VectorPropertyViewModel
	{
		public override bool IsFullWidth => true;

		public Vec3PropertyViewModel(IVectorProperty property)
			: base(property)
		{
			Trace.Assert(property.NumComponents == 3);
		}
	}

	// Factory for vector property view models.
	class Vec3PropertyViewModelFactory : IPropertyViewModelFactory
	{
		public int Priority => 10;

		public bool SupportsProperty(IUserProperty property)
		{
			var vecProperty = property as IVectorProperty;
			return vecProperty != null && vecProperty.NumComponents == 3
				&& vecProperty.GetComponents().All(component => component is IScalarProperty<float>);
		}

		public PropertyViewModel CreateInstance(IUserProperty property) => new Vec3PropertyViewModel((IVectorProperty)property);
	}
}
