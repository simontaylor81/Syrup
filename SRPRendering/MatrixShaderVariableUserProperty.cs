using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.D3DCompiler;
using SRPCommon.UserProperties;
using System.Reactive;
using System.Reactive.Subjects;

namespace SRPRendering
{
	class MatrixShaderVariableUserProperty : IMatrixProperty
	{
		public MatrixShaderVariableUserProperty(IShaderVariable variable, IUserProperty[,] components)
		{
			// TODO: Row-major?
			Debug.Assert(variable.VariableType.Class == ShaderVariableClass.MatrixColumns);

			Debug.Assert(components.GetLength(0) == variable.VariableType.Columns);
			Debug.Assert(components.GetLength(1) == variable.VariableType.Rows);

			this._components = components;
			this._variable = variable;
		}

		public string Name => _variable.Name;
		public bool IsReadOnly => false;
		public int NumColumns => _components.GetLength(0);
		public int NumRows => _components.GetLength(1);

		public IUserProperty GetComponent(int row, int col) => _components[col, row];

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			// We change when the underlying variable changes.
			return _variable.Subscribe(observer);
		}

		private IUserProperty[,] _components;
		private IShaderVariable _variable;
	}
}
