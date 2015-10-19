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
		private readonly IShaderVariable _variable;
		int _componentIndex;

		public FloatShaderVariableUserProperty(IShaderVariable variable, int componentIndex)
		{
			_variable = variable;
			_componentIndex = componentIndex;
		}

		public bool IsReadOnly => false;
		public string Name => _variable.Name;

		public float Value
		{
			get
			{
				return _variable.GetComponent<float>(_componentIndex);
			}
			set
			{
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
				if (value != Value)
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
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
