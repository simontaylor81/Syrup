using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;

namespace SRPRendering
{
	// Utilities related to SharpDX.
	public static class SharpDXUtils
	{
		// Get all constant buffers from shader reflection.
		public static IEnumerable<SharpDX.D3DCompiler.ConstantBuffer> GetConstantBuffers(this ShaderReflection shaderReflection)
		{
			return Enumerable.Range(0, shaderReflection.Description.ConstantBuffers)
				.Select(i => shaderReflection.GetConstantBuffer(i));
		}

		// Get all bound resources from shader reflection.
		public static IEnumerable<InputBindingDescription> GetBoundResources(this ShaderReflection shaderReflection)
		{
			return Enumerable.Range(0, shaderReflection.Description.BoundResources)
				.Select(i => shaderReflection.GetResourceBindingDescription(i));
		}

		// Get all the variables descriptors for a shader reflection constant buffer descriptor.
		public static IEnumerable<ShaderReflectionVariable> GetVariables(this SharpDX.D3DCompiler.ConstantBuffer cbuffer)
		{
			return Enumerable.Range(0, cbuffer.Description.VariableCount)
				.Select(i => cbuffer.GetVariable(i));
		}
	}
}
