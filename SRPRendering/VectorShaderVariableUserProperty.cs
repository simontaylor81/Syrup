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
			//Debug.Assert(variable.VariableType.Type == ShaderVariableType.Float);

			Debug.Assert(components.Length == variable.VariableType.Columns * variable.VariableType.Rows);

			this._components = components;
			this._variable = variable;
		}

		public string Name { get { return _variable.Name; } }
		public bool IsReadOnly { get { return false; } }
		public int NumComponents { get { return _components.Length; } }

		public IUserProperty GetComponent(int index)
		{
			return _components[index];
		}

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			// We change when the underlying variable changes.
			// TODO: Subscribe to merged sub-components?
			return _variable.Subscribe(observer);
		}

		private IUserProperty[] _components;
		private IShaderVariable _variable;
	}
}
