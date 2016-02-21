using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPScripting
{
	public enum ShaderVariableBindSource
	{
		WorldToProjectionMatrix,	// Bind to the combined world to projection space matrix.
		ProjectionToWorldMatrix,	// Bind to the combined projection to world space matrix (i.e. the inverse of the WorldToProjectionMatrix).
		LocalToWorldMatrix,			// Bind to the object-local to world space matrix.
		WorldToLocalMatrix,			// Bind to the world to object-local space matrix (i.e. the inverse of the local to world matrix).
		LocalToWorldInverseTransposeMatrix,	// Bind to the inverse-transpose of the local-to-world matrix.
		CameraPosition,				// Bind to the position of the camera in world-space.
	}

	// Delegate type for the per-frame callback. Cannot be inside the interface cos C# is silly.
	public delegate void FrameCallback(IRenderContext context);

	// Interface to the rendering system exposed to the scripting system.
	public interface IRenderInterface
	{
		object CompileShader(string filename, string entryPoint, string profile,
			IDictionary<string, object> defines = null);

		// Compile a shader from an in-memory string.
		// All includes still must come from the file system.
		object CompileShaderFromString(string source, string entryPoint, string profile,
			IDictionary<string, object> defines = null);

		// Create a render target of dimensions equal to the viewport.
		object CreateRenderTarget();

		// Create a 2D texture of the given size and format, and fill it with the given data.
		object CreateTexture2D(int width, int height, Format format, dynamic contents, bool generateMips = false);

		// Create a buffer of the given size and format, and fill it with the given data.
		IBuffer CreateBuffer(int sizeInBytes, Format format, dynamic contents, bool uav = false);

		// Create a structured buffer.
		IBuffer CreateStructuredBuffer<T>(IEnumerable<T> contents, bool uav = false) where T : struct;

		// Load a texture from a file.
		object LoadTexture(string path, object generateMips = null);

		void BindShaderVariable(object shader, string var, ShaderVariableBindSource source);
		void BindShaderVariableToMaterial(object shader, string var, string param);
		void SetShaderVariable(object shader, string var, dynamic value);
		void ShaderVariableIsScriptOverride(object shader, string var);

		void BindShaderResourceToMaterial(object shader, string var, string param, object fallback = null);
		void SetShaderResourceVariable(object shader, string var, object value);
		void SetShaderSamplerState(object shader, string samplerName, SamplerState state);
		void SetShaderUavVariable(object shader, string var, IBuffer value);

		#region User Variables
		dynamic AddUserVar_Float(string name, float defaultValue);
		dynamic AddUserVar_Float2(string name, object defaultValue);
		dynamic AddUserVar_Float3(string name, object defaultValue);
		dynamic AddUserVar_Float4(string name, object defaultValue);
		dynamic AddUserVar_Int(string name, int defaultValue);
		dynamic AddUserVar_Int2(string name, object defaultValue);
		dynamic AddUserVar_Int3(string name, object defaultValue);
		dynamic AddUserVar_Int4(string name, object defaultValue);
		dynamic AddUserVar_Bool(string name, bool defaultValue);
		dynamic AddUserVar_String(string name, string defaultValue);
		dynamic AddUserVar_Choice(string name, IEnumerable<object> choices, object defaultValue);
		#endregion

		// Still unsure if this is the best way to go.
		void SetFrameCallback(FrameCallback callback);

		// Get access to the scene.
		dynamic GetScene();

		// Handles to special resources.
		//object BackBuffer { get; }
		object DepthBuffer { get; }
		object NoDepthBuffer { get; }

		object BlackTexture { get; }
		object WhiteTexture { get; }
		object DefaultNormalTexture { get; }
	}
}
