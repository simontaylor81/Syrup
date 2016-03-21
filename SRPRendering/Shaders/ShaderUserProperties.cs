using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SRPCommon.Scripting;
using SRPCommon.UserProperties;

namespace SRPRendering.Shaders
{
	static class ShaderUserProperties
	{
		public static IUserProperty Create(IEnumerable<ShaderConstantVariable> variables)
		{
			// Convert to list to avoid multiple potentially expensive iterations.
			variables = variables.ToList();

			// Must have at least one element.
			var first = variables.First();

			// Must be all the same type.
			if (!variables.All(v => v.VariableType.Equals(first.VariableType)))
			{
				throw new ScriptException($"Shader variables named '{first.Name}' do not all share the same type.");
			}

			switch (first.VariableType.Class)
			{
				case ShaderVariableClass.Vector:
					{
						int numComponents = first.VariableType.Rows * first.VariableType.Columns;
						var components = Enumerable.Range(0, numComponents)
							.Select(i => CreateScalar(variables, i))
							.ToArray();
						return new VectorShaderVariableUserProperty(variables, components);
					}

				case ShaderVariableClass.MatrixColumns:
					{
						// Save typing
						var numCols = first.VariableType.Columns;
						var numRows = first.VariableType.Rows;

						// Create a scalar property for each element in the matrix.
						var components = new IUserProperty[numCols, numRows];
						for (int col = 0; col < numCols; col++)
						{
							for (int row = 0; row < numRows; row++)
							{
								components[col, row] = CreateScalar(variables, row + col * numRows);
							}
						}

						// Create matrix property.
						return new MatrixShaderVariableUserProperty(variables, components);
					}

				case ShaderVariableClass.Scalar:
					return CreateScalar(variables, 0);
			}

			throw new ScriptException("Unsupported shader parameter type. Variable: " + first.Name);
		}

		private static IUserProperty CreateScalar(IEnumerable<ShaderConstantVariable> variables, int componentIndex)
		{
			switch (variables.First().VariableType.Type)
			{
				case ShaderVariableType.Float:
					return new ScalarShaderVariableUserProperty<float>(variables, componentIndex);
				case ShaderVariableType.Int:
					return new ScalarShaderVariableUserProperty<int>(variables, componentIndex);
			}

			throw new ScriptException("Unsupported shader parameter type. Variable: " + variables.First());
		}
	}
}
