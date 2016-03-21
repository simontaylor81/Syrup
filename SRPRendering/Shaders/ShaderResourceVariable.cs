using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SRPCommon.Scripting;
using SRPRendering.Resources;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPRendering.Shaders
{
	class ShaderResourceVariable : IShaderResourceVariable
	{
		// IShaderVariable interface.
		public string Name { get; }
		public bool IsNull => false;

		#region IShaderResourceVariable interface.

		// Set directly to a given resource.
		public void Set(IShaderResource iresource)
		{
			var resource = iresource as ID3DShaderResource;
			if (resource == null)
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

		public void SetToDevice(DeviceContext context, IPrimitive primitive, ViewInfo viewInfo, IGlobalResources globalResources)
		{
			var resource = Binding?.GetResource(primitive, viewInfo, globalResources);

			switch (shaderFrequency)
			{
				case ShaderFrequency.Vertex:
					context.VertexShader.SetShaderResource(slot, resource);
					break;

				case ShaderFrequency.Pixel:
					context.PixelShader.SetShaderResource(slot, resource);
					break;

				case ShaderFrequency.Compute:
					context.ComputeShader.SetShaderResource(slot, resource);
					break;
			}
		}

		public void Reset()
		{
			// Set backing var directly to bypass 'already set' check.
			_binding = null;
		}

		// Constructors.
		public ShaderResourceVariable(InputBindingDescription desc, ShaderFrequency shaderFrequency)
		{
			Name = desc.Name;
			slot = desc.BindPoint;

			this.shaderFrequency = shaderFrequency;

			// TODO: Support arrays.
			Trace.Assert(desc.BindCount == 1);
		}

		private int slot;
		private ShaderFrequency shaderFrequency;
	}
}
