﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scripting;
using SRPRendering.Resources;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	// Dummy shader resource variable to give to script before shaders are compiled.
	class ShaderResourceVariableHandle : IShaderResourceVariable
	{
		public string Name { get; }

		#region IShaderResourceVariable interface.

		// Set directly to a given resource.
		public void Set(IShaderResource iresource)
		{
			var resource = iresource as ID3DShaderResource;
			if (iresource != null && resource == null)
			{
				throw new ScriptException("Invalid shader resource");
			}

			Binding = new DirectShaderResourceVariableBinding(resource);
		}

		// Bind to a material property.
		public void BindToMaterial(string param, IShaderResource fallback = null)
		{
			var fallbackResource = fallback as ID3DShaderResource;
			if (fallback != null && fallbackResource == null)
			{
				throw new ScriptException("Invalid fallback resource");
			}

			Binding = new MaterialShaderResourceVariableBinding(param, fallbackResource);
		}

		#endregion

		private IShaderResourceVariableBinding _binding;
		public IShaderResourceVariableBinding Binding
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

		public ShaderResourceVariableHandle(string name)
		{
			Name = name;
		}
	}
}