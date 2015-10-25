using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;

namespace SRPRendering
{
	class FloatShaderVariableUserProperty : IScalarProperty<float>
	{
		private readonly IEnumerable<IShaderVariable> _variables;
		int _componentIndex;

		public FloatShaderVariableUserProperty(IEnumerable<IShaderVariable> variables, int componentIndex)
		{
			_variables = variables;
			_componentIndex = componentIndex;

			// Different variables may have different defaults,
			// so force them all to the same value now.
			var firstValue = variables.First().GetComponent<float>(componentIndex);
			foreach (var variable in variables.Skip(1))
			{
				variable.SetComponent(_componentIndex, firstValue);
			}
		}

		public bool IsReadOnly => false;
		public string Name => _variables.First().Name;

		public Type Type => typeof(float);

		public float Value
		{
			get
			{
				// Assume everything has the same value.
				return _variables.First().GetComponent<float>(_componentIndex);
			}
			set
			{
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
				if (value != Value)
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
				{
					// Set on all variables.
					foreach (var variable in _variables)
					{
						variable.SetComponent(_componentIndex, value);
					}
				}
			}
		}

		public IDisposable Subscribe(IObserver<Unit> observer)
		{
			return _variables.Merge().Subscribe(observer);
		}
	}
}
