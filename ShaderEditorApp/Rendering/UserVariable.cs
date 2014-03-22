using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.ViewModel;
using SRPScripting;

namespace ShaderEditorApp.Rendering
{
	abstract class UserVariable
	{
		public string Name { get; private set; }
		public abstract dynamic GetFunction();
		public abstract PropertyViewModel CreateViewModel();

		public static UserVariable Create(string name, UserVariableType type, dynamic defaultValue)
		{
			try
			{
				switch (type)
				{
					// Float scalar & vectors.
					case UserVariableType.Float:
						return new UserVariableFloat(1, name, defaultValue);
					case UserVariableType.Float2:
						return new UserVariableFloat(2, name, defaultValue);
					case UserVariableType.Float3:
						return new UserVariableFloat(3, name, defaultValue);
					case UserVariableType.Float4:
						return new UserVariableFloat(4, name, defaultValue);

					case UserVariableType.Bool:
						return new UserVariableBool(name, defaultValue);

					default:
						throw new ArgumentException("Invalid user variable type.");
				}
			}
			catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
			{
				throw new ScriptException("Incorrect type for user variable default value.", ex);
			}
		}

		protected UserVariable(string name)
		{
			this.Name = name;
		}
	}

	// User variable representing a floating point vector or scalar with a variable number of components.
	class UserVariableFloat : UserVariable
	{
		public override dynamic GetFunction()
		{
			// Special handling for scalars: return a function returning a float, not an array of a single float.
			if (NumComponents == 1)
			{
				Func<float> func = () => values[0];
				return func;
			}
			else
			{
				Func<IEnumerable<float>> func = () => values;
				return func;
			}
		}

		public override PropertyViewModel CreateViewModel()
		{
			return new UserVariableFloatVectorViewModel(this);
		}

		public UserVariableFloat(int numComponents, string name, dynamic defaultValue)
			: base(name)
		{
			this.numComponents = numComponents;

			// Special handling for scalars, as we want to pass a float, not a list of floats.
			if (numComponents == 1)
			{
				// Use explicit cast to convert from ints/doubles.
				float value = (float)defaultValue;
				values = new[] { value };
			}
			else
			{
				// Get values from default.
				values = new float[numComponents];
				for (int i = 0; i < numComponents; i++)
				{
					values[i] = (float)defaultValue[i];
				}
			}
		}

		public float this[int index]
		{
			get { return values[index]; }
			set { values[index] = value;  }
		}

		// The number of components in the vector.
		private readonly int numComponents;
		public int NumComponents { get { return numComponents; } }

		// The storage of the actual values.
		private float[] values;
	}

	class UserVariableBool : UserVariable
	{
		public override dynamic GetFunction()
		{
			Func<bool> func = () => Value;
			return func;
		}

		public override PropertyViewModel CreateViewModel()
		{
			return new UserVariableBoolViewModel(this);
		}

		public UserVariableBool(string name, bool defaultValue)
			: base(name)
		{
			Value = defaultValue;
		}

		public bool Value { get; set; }
	}
}
