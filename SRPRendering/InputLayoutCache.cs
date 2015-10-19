using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SRPCommon.Scripting;

namespace SRPRendering
{
	public interface IInputLayoutCache : IDisposable
	{
		InputLayout GetInputLayout(Device device, ShaderSignature shaderSignature, InputElement[] inputElements);
	}

	class InputLayoutCache : IInputLayoutCache
	{
		public InputLayout GetInputLayout(Device device, ShaderSignature shaderSignature, InputElement[] inputElements)
		{
			InputLayout result;
			var key = new Key() { shaderSignature = shaderSignature, inputElements = inputElements };

			// Check the cache for an appropriate input layout.
			if (entries.TryGetValue(key, out result))
			{
				return result;
			}

			// None found, create a new one.
			try
			{
				result = new InputLayout(device, shaderSignature, inputElements);
				entries.Add(key, result);
				return result;
			}
			catch (Direct3D11Exception ex)
			{
				throw new ScriptException("Failed to create input layout. Check shader inputs match scene format.", ex);
			}
		}

		public void Dispose()
		{
			// Dispose all the input layouts we've created.
			foreach (var inputLayout in entries.Values)
				inputLayout.Dispose();

			entries.Clear();
		}

		private struct Key : IEquatable<Key>
		{
			public ShaderSignature shaderSignature;
			public InputElement[] inputElements;

			public bool Equals(Key other)
				=> shaderSignature == other.shaderSignature &&
					inputElements.SequenceEqual(other.inputElements);
		}

		private Dictionary<Key, InputLayout> entries = new Dictionary<Key, InputLayout>();
	}
}
