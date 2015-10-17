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
	class VectorShaderVariableUserProperty : IVectorProperty
	{
		public VectorShaderVariableUserProperty(IShaderVariable variable, IUserProperty[] components)
		{
			Debug.Assert(variable.VariableType.Class == ShaderVariableClass.Vector ||
						 variable.VariableType.Class == ShaderVariableClass.MatrixColumns);

			Debug.Assert(components.Length == variable.VariableType.Columns * variable.VariableType.Rows);

			this._components = components;
			this._variable = variable;
		}

		public string Name => _variable.Name;
		public bool IsReadOnly => false;

		public int NumComponents => _components.Length;
		public IUserProperty GetComponent(int index) => _components[index];

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			// We change when the underlying variable changes.
			return _variable.Subscribe(observer);
		}

		private IUserProperty[] _components;
		private IShaderVariable _variable;
	}
}
