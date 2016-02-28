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
	class VectorShaderVariableUserProperty : IVectorProperty
	{
		public VectorShaderVariableUserProperty(IEnumerable<IShaderVariable> variables, IUserProperty[] components)
		{
			var first = variables.First();
			Debug.Assert(first.VariableType.Class == ShaderVariableClass.Vector ||
						 first.VariableType.Class == ShaderVariableClass.MatrixColumns);

			Debug.Assert(components.Length == first.VariableType.Columns * first.VariableType.Rows);

			this._components = components;
			this._variables = variables;
		}

		public string Name => _variables.First().Name;
		public bool IsReadOnly => false;

		public int NumComponents => _components.Length;
		public IUserProperty GetComponent(int index) => _components[index];

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			// We change when any of the underlying variables change.
			return _variables.Merge().Subscribe(observer);
		}

		private readonly IUserProperty[] _components;
		private readonly IEnumerable<IShaderVariable> _variables;
	}
}
