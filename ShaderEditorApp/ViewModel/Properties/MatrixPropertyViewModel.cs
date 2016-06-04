using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.ViewModel.Properties
{
	// View-model class for vector properties. I.e. those with multiple values of a particular type.
	class MatrixPropertyViewModel : PropertyViewModel
	{
		public MatrixPropertyViewModel(IMatrixProperty property)
			: base(property)
		{
			Rows = Enumerable.Range(0, property.NumRows)
				.Select(row =>
					Enumerable.Range(0, property.NumColumns)
						.Select(col => PropertyViewModelFactory.CreateViewModel(property.GetComponent(row, col)))
						.ToArray()
				)
				.ToArray();
		}

		public PropertyViewModel[][] Rows { get; }
	}

	// Factory for choice property view models.
	class MatrixPropertyViewModelFactory : IPropertyViewModelFactory
	{
		public int Priority => 10;
		public bool SupportsProperty(IUserProperty property) => property is IMatrixProperty;
		public PropertyViewModel CreateInstance(IUserProperty property) => new MatrixPropertyViewModel((IMatrixProperty)property);
	}
}
