using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;

namespace SRPRendering
{
	class FloatShaderVariableUserProperty : IScalarProperty<float>
	{
		private IShaderVariable _variable;
		int _componentIndex;

		public FloatShaderVariableUserProperty(IShaderVariable variable, int componentIndex)
		{
			_variable = variable;
			_componentIndex = componentIndex;
		}

		public bool IsReadOnly { get { return false; } }

		public string Name { get { return _variable.Name; } }

		public float Value
		{
			get
			{
				return _variable.GetComponent<float>(_componentIndex);
			}
			set
			{
				if (value != Value)
				{
					_variable.SetComponent(_componentIndex, value);
				}
			}
		}

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _variable.Subscribe(observer);
		}
	}
}
