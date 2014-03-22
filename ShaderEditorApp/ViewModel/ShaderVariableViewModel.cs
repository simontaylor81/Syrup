using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.Rendering;
using SlimDX.D3DCompiler;

namespace ShaderEditorApp.ViewModel
{
	class VectorShaderVariableViewModel<T> : VectorPropertyBase<T> where T : struct
	{
		public VectorShaderVariableViewModel(IShaderVariable variable)
			: base(variable.Name, variable.VariableType.Columns * variable.VariableType.Rows)
		{
			Debug.Assert(variable.VariableType.Class == ShaderVariableClass.Vector ||
						 variable.VariableType.Class == ShaderVariableClass.Scalar ||
						 variable.VariableType.Class == ShaderVariableClass.MatrixColumns);
			Debug.Assert(variable.VariableType.Type == ShaderVariableType.Float);

			this.variable = variable;

			// Hook the variable's ValueChanged event so external modifications (e.g. scene bindings)
			// update the value in the UI.
			variable.ValueChanged += () => OnPropertyChanged("Item[]");
		}

		public override T this[int index]
		{
			get
			{
				return variable.GetComponent<T>(index);
			}
			set
			{
				if (!value.Equals(this[index]))
				{
					variable.SetComponent(index, value);
					OnPropertyChanged("Item[]");
				}
			}
		}

		private IShaderVariable variable;
	}
}
