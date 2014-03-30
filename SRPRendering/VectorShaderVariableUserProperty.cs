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
	class VectorShaderVariableUserProperty<T> : IVectorProperty<T> where T : struct
	{
		public VectorShaderVariableUserProperty(IShaderVariable variable)
		{
			Debug.Assert(variable.VariableType.Class == ShaderVariableClass.Vector ||
						 variable.VariableType.Class == ShaderVariableClass.Scalar ||
						 variable.VariableType.Class == ShaderVariableClass.MatrixColumns);
			Debug.Assert(variable.VariableType.Type == ShaderVariableType.Float);

			this._variable = variable;
		}

		public string Name { get { return _variable.Name; } }
		public bool IsReadOnly { get { return false; } }
		public int NumComponents { get { return _variable.VariableType.Columns * _variable.VariableType.Rows; } }

		public T this[int index]
		{
			get
			{
				return _variable.GetComponent<T>(index);
			}
			set
			{
				if (!value.Equals(this[index]))
				{
					_variable.SetComponent(index, value);
				}
			}
		}

		public IDisposable Subscribe(IObserver<System.Reactive.Unit> observer)
		{
			// We change when the underlying variable changes.
			return _variable.Subscribe(observer);
		}

		private IShaderVariable _variable;
	}
}
