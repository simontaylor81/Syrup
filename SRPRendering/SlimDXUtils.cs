using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;

namespace SRPRendering
{
	// Utilities related to SlimDX.
	public static class SlimDXUtils
	{
		// Is the given format (block) compressed?
		// C# version of function found in SlimDX's Utilities.cpp (which sadly is an internal class).
		public static bool IsCompressed(Format format)
		{
			switch (format)
			{
				case Format.BC1_Typeless:
				case Format.BC1_UNorm:
				case Format.BC1_UNorm_SRGB:
				case Format.BC4_Typeless:
				case Format.BC4_UNorm:
				case Format.BC4_SNorm:
				case Format.BC2_Typeless:
				case Format.BC2_UNorm:
				case Format.BC2_UNorm_SRGB:
				case Format.BC3_Typeless:
				case Format.BC3_UNorm:
				case Format.BC3_UNorm_SRGB:
				case Format.BC5_Typeless:
				case Format.BC5_UNorm:
				case Format.BC5_SNorm:
				case Format.BC6_UFloat16:
				case Format.BC6_SFloat16:
				case Format.BC6_Typeless:
				case Format.BC7_UNorm:
				case Format.BC7_UNorm_SRGB:
				case Format.BC7_Typeless:
					return true;
			}

			return false;
		}

		// Get all constant buffers from shader reflection.
		public static IEnumerable<SlimDX.D3DCompiler.ConstantBuffer> GetConstantBuffers(this ShaderReflection shaderReflection)
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
	}
}
