using System;
using System.Text;
using SlimDX;

namespace ShaderUnit.TestRenderer
{
	class HlslTestHarness
	{
		// Use weird names to reduce chance of conflicts with the SUT.
		public static string EntryPoint => "__Main";
		public static string OutBufferName => "__OutputBuffer";

		public static string GenerateComputeShader(string shaderFile, string function, Type returnType, object[] parameters)
		{
			var result = new StringBuilder();

			var hlslReturnType = ClrTypeToHlsl(returnType);

			result.AppendLine($"#include \"{shaderFile}\"");
			result.AppendLine($"RWStructuredBuffer<{hlslReturnType}> {OutBufferName};");
			result.AppendLine("[numthreads(1, 1, 1)]");
			result.AppendLine($"void {EntryPoint}()");
			result.AppendLine("{");
			result.AppendLine($"\t{OutBufferName}[0] = {function}();");			// TODO: Parameters.
			result.AppendLine("}");

			return result.ToString();
		}

		public static string ClrTypeToHlsl(Type type)
		{
			if (type == typeof(float))
			{
				return "float";
			}
			else if (type == typeof(int))
			{
				return "int";
			}
			else if (type == typeof(uint))
			{
				return "uint";
			}
			else if (type == typeof(Vector2))
			{
				return "float2";
			}
			else if (type == typeof(Vector3))
			{
				return "float3";
			}
			else if (type == typeof(Vector4))
			{
				return "float4";
			}

			throw new ArgumentException($"Type cannot be converted to HLSL: {type.ToString()}", nameof(type));
		}
	}
}
