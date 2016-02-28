using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SRPCommon.UserProperties;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace SRPRendering
{
	class MatrixShaderVariableUserProperty : IMatrixProperty
	{
		public MatrixShaderVariableUserProperty(IEnumerable<IShaderVariable> variables, IUserProperty[,] components)
		{
			var first = variables.First();

			// TODO: Row-major?
			Debug.Assert(first.VariableType.Class == ShaderVariableClass.MatrixColumns);

			Debug.Assert(components.GetLength(0) == first.VariableType.Columns);
			Debug.Assert(components.GetLength(1) == first.VariableType.Rows);

			_components = components;
			_variables = variables;
		}

		public string Name => _variables.First().Name;
		public bool IsReadOnly => false;
		public int NumColumns => _components.GetLength(0);
		public int NumRows => _components.GetLength(1);

		public IUserProperty GetComponent(int row, int col) => _components[col, row];

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			// We change when the underlying variable changes.
			return _variables.Merge().Subscribe(observer);
		}

		private readonly IUserProperty[,] _components;
		private readonly IEnumerable<IShaderVariable> _variables;
	}
}
