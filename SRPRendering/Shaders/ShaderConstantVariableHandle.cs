using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scripting;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	// Dummy shader constant variable to give to script before shaders are compiled.
	class ShaderConstantVariableHandle : IShaderConstantVariable
	{
		public string Name { get; }

		public ShaderConstantVariableHandle(string name)
		{
			Name = name;
		}

		#region IShaderConstantVariable interface

		// Set directly to a given value.
		public void Set(dynamic value)
		{
			Binding = new ScriptShaderConstantVariableBinding(value);
		}

		// Bind to camera/scene property.
		public void Bind(ShaderConstantVariableBindSource bindSource)
		{
			Binding = new SimpleShaderConstantVariableBinding(bindSource);
		}

		// Bind to a material property.
		public void BindToMaterial(string param)
		{
			Binding = new MaterialShaderConstantVariableBinding(param);
		}

		// Mark the variable as script overridden, so it will not appear in the properties window.
		public void MarkAsScriptOverride()
		{
			Binding = new ScriptOverrideShaderConstantVariableBinding();
		}

		#endregion

		public ShaderVariableTypeDesc VariableType { get; }

		private IShaderConstantVariableBinding _binding;
		public IShaderConstantVariableBinding Binding
		{
			get { return _binding; }
			private set
			{
				if (_binding != null)
				{
					throw new ScriptException("Attempting to bind already bound shader variable: " + Name);
				}
				_binding = value;
			}
		}
	}
}
