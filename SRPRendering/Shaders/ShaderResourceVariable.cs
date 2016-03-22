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
	class ShaderResourceVariable
	{
		public string Name { get; }
		public IShaderResourceVariableBinding Binding { get; set; }

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
			Binding = null;
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
