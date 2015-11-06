using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.ViewModel
{
	static class PropertyViewModelFactory
	{
		// Create a new view-model object for a user property.
		public static PropertyViewModel CreateViewModel(IUserProperty property)
		{
			// This is a bit nasty, would rather have something more generic.
			if (property is IChoiceProperty)
				return new ChoicePropertyViewModel((IChoiceProperty)property);
			if (property is IScalarProperty<float>)
				return new ScalarPropertyViewModel<float>((IScalarProperty<float>)property);
			if (property is IScalarProperty<bool>)
				return new ScalarPropertyViewModel<bool>((IScalarProperty<bool>)property);
			if (property is IScalarProperty<string>)
				return new ScalarPropertyViewModel<string>((IScalarProperty<string>)property);
			if (property is IScalarProperty<int>)
				return new ScalarPropertyViewModel<int>((IScalarProperty<int>)property);
			if (property is IVectorProperty)
				return new VectorPropertyViewModel((IVectorProperty)property);
			if (property is IMatrixProperty)
				return new MatrixPropertyViewModel((IMatrixProperty)property);

			throw new ArgumentException("Unsupported property type");
		}
	}
}
