using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
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
			var key = new Key(shaderSignature, inputElements);

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
			catch (SharpDXException ex)
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

		// Array of input element structures that describe the layout of SceneVertex to D3D.
		// Doesn't really belong here, but can't go with SceneVertex because it's D3D-specific.
		// This seems as good a place as anywhere.
		public static InputElement[] SceneVertexInputElements => new[]
		{
			new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
			new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
			new InputElement("TANGENT", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
			new InputElement("BITANGENT", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
			new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 0),
			new InputElement("TEXCOORD", 1, SharpDX.DXGI.Format.R32G32_Float, 0),
			new InputElement("TEXCOORD", 2, SharpDX.DXGI.Format.R32G32_Float, 0),
			new InputElement("TEXCOORD", 3, SharpDX.DXGI.Format.R32G32_Float, 0)
		};

		private struct Key : IEquatable<Key>
		{
			public readonly ShaderSignature shaderSignature;
			public readonly InputElement[] inputElements;

			public Key(ShaderSignature shaderSignature, InputElement[] inputElements)
			{
				this.shaderSignature = shaderSignature;
				this.inputElements = inputElements;
			}

			public bool Equals(Key other)
				=> object.ReferenceEquals(shaderSignature, other.shaderSignature) &&
					inputElements.SequenceEqual(other.inputElements);

			public override int GetHashCode()
			{
				// Don't use default hash function because it uses the internal hash
				// of ShaderSignature, which can crash if it's been disposed.
				return inputElements.Aggregate(
					RuntimeHelpers.GetHashCode(shaderSignature),
					(hash, element) => hash ^ element.GetHashCode()
					);
			}
		}

		private Dictionary<Key, InputLayout> entries = new Dictionary<Key, InputLayout>();
	}
}
