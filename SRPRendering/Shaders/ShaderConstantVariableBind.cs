﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SRPScripting;
using SRPCommon.Scripting;
using System.Numerics;
using SRPCommon.Util;

namespace SRPRendering.Shaders
{
	interface IShaderConstantVariableBinding
	{
		void UpdateVariable(ShaderConstantVariable variable, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, object> overrides);
		bool AllowScriptOverride { get; }
	}

	class SimpleShaderConstantVariableBinding : IShaderConstantVariableBinding
	{
		public SimpleShaderConstantVariableBinding(ShaderConstantVariableBindSource source)
		{
			this.source = source;
		}

		public void UpdateVariable(ShaderConstantVariable variable, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, object> overrides)
		{
			switch (source)
			{
				case ShaderConstantVariableBindSource.WorldToProjectionMatrix:
					variable.SetValue(viewInfo.WorldToViewMatrix * viewInfo.ViewToProjMatrix);
					return;

				case ShaderConstantVariableBindSource.ProjectionToWorldMatrix:
					{
						var matrix = viewInfo.WorldToViewMatrix * viewInfo.ViewToProjMatrix;
						Matrix4x4.Invert(matrix, out matrix);
						variable.SetValue(matrix);
					}
					return;

				case ShaderConstantVariableBindSource.LocalToWorldMatrix:
					if (primitive != null)
					{
						variable.SetValue(primitive.LocalToWorld);
						return;
					}
					break;

				case ShaderConstantVariableBindSource.WorldToLocalMatrix:
					if (primitive != null)
					{
						var matrix = primitive.LocalToWorld;
						Matrix4x4.Invert(matrix, out matrix);
						variable.SetValue(matrix);
						return;
					}
					break;

				case ShaderConstantVariableBindSource.LocalToWorldInverseTransposeMatrix:
					if (primitive != null)
					{
						var matrix = primitive.LocalToWorld;
						Matrix4x4.Invert(matrix, out matrix);
						variable.SetValue(Matrix4x4.Transpose(matrix));
						return;
					}
					break;

				case ShaderConstantVariableBindSource.CameraPosition:
					variable.SetValue(viewInfo.EyePosition);
					return;
			}

			// If we got this far, the variable was not set, so fall back to the default.
			variable.SetDefault();
		}

		public bool AllowScriptOverride => false;

		private ShaderConstantVariableBindSource source;
	}

	class ScriptShaderConstantVariableBinding : IShaderConstantVariableBinding
	{
		public ScriptShaderConstantVariableBinding(object value)
		{
			this.value = value;
		}

		public void UpdateVariable(ShaderConstantVariable variable, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, object> overrides)
		{
			// Check type of the variable.
			if (variable.VariableType.Type == ShaderVariableType.Float)
			{
				// Check that the script gave us the correct type.
				int numComponents = variable.VariableType.Columns * variable.VariableType.Rows;
				ScriptHelper.CheckConvertibleFloatList(value, numComponents,
					String.Format("Value for shader variable '{0}'", variable.Name));
			}
			else
			{
				// TODO: Support other variable types.
				throw new ScriptException("Unsupported shader variable type: " + variable.VariableType.Type.ToString());
			}

			try
			{
				// If the script gave us a function, call it.
				dynamic val = ScriptHelper.ResolveFunction(value);
				variable.SetFromDynamic(val);
			}
			catch (ScriptException ex)
			{
				throw new ScriptException("Incorrect type for bound shader variable:" + variable.Name, ex);
			}
		}

		public bool AllowScriptOverride => false;

		private dynamic value;
	}

	class MaterialShaderConstantVariableBinding : IShaderConstantVariableBinding
	{
		public MaterialShaderConstantVariableBinding(string source)
		{
			this.source = source;
		}

		public void UpdateVariable(ShaderConstantVariable variable, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, object> overrides)
		{
			if (variable.VariableType.Type != ShaderVariableType.Float)
			{
				throw new ScriptException(String.Format("Cannot bind shader variable '{0}' to material parameter: only float parameters are supported.", variable.Name));
			}

			if (primitive != null && primitive.Material != null)
			{
				Vector4 value;
				if (primitive.Material.Parameters.TryGetValue(source, out value))
				{
					var valueArray = value.ToArray();

					// Set each component individually, as the variable might be < 4 components.
					int numComponents = Math.Min(variable.VariableType.Columns * variable.VariableType.Rows, 4);
					for (int i = 0; i < numComponents; i++)
					{
						variable.SetComponent(i, valueArray[i]);
					}

					return;
				}
			}

			// Could not get value from material, so reset to default.
			variable.SetDefault();
		}

		public bool AllowScriptOverride => false;

		private string source;
	}

	class ScriptOverrideShaderConstantVariableBinding : IShaderConstantVariableBinding
	{
		public void UpdateVariable(ShaderConstantVariable variable, ViewInfo viewInfo, IPrimitive primitive, IDictionary<string, object> overrides)
		{
			object overriddenValue;

			// Is the variable overridden this drawcall?
			if (overrides != null && overrides.TryGetValue(variable.Name, out overriddenValue))
			{
				try
				{
					variable.SetFromDynamic(overriddenValue);
				}
				catch (ScriptException ex)
				{
					throw new ScriptException("Incorrect type for shader variable override: " + variable.Name, ex);
				}
			}
			else
			{
				// Not overridden, so set to default.
				variable.SetDefault();
			}
		}

		public bool AllowScriptOverride => true;
	}
}
