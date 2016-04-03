using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPScripting.Shader;

namespace SRPScripting
{
	public enum ShaderConstantVariableBindSource
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

	// Interface to the rendering system exposed to statically-typed scripting languages (i.e. C#).
	public interface IRenderInterface
	{
		IShader CompileShader(string filename, string entryPoint, string profile,
			IDictionary<string, object> defines = null);

		// Create a render target of dimensions equal to the viewport.
		IRenderTarget CreateRenderTarget();

		// Create a 2D texture of the given size and format, and fill it with the given data.
		ITexture2D CreateTexture2D<T>(int width, int height, Format format, IEnumerable<T> contents);

		// Create a 2D texture of the given size and format, and fill it with data from the given callback.
		ITexture2D CreateTexture2D(int width, int height, Format format, Func<int, int, object> contentCallback);

		// Create a structured buffer containing the given contents exactly as it is.
		IBuffer CreateStructuredBuffer<T>(IEnumerable<T> contents) where T : struct;

		// Create an buffer of the given size and format, optionally with initial data that is converted to the correct format.
		IBuffer CreateFormattedBuffer<T>(int numElements, Format format, IEnumerable<T> contents);

		// Create an uninitialised buffer of the given size and format, to be written to by the GPU.
		IBuffer CreateUninitialisedBuffer(int sizeInBytes, int stride);

		// Load a texture from a file.
		ITexture2D LoadTexture(string path);

		#region User Variables
		// TODO: Stronger typing on default and returned functions.
		Func<float> AddUserVar_Float(string name, float defaultValue);
		Func<float[]> AddUserVar_Float2(string name, object defaultValue);
		Func<float[]> AddUserVar_Float3(string name, object defaultValue);
		Func<float[]> AddUserVar_Float4(string name, object defaultValue);
		Func<int> AddUserVar_Int(string name, int defaultValue);
		Func<int[]> AddUserVar_Int2(string name, object defaultValue);
		Func<int[]> AddUserVar_Int3(string name, object defaultValue);
		Func<int[]> AddUserVar_Int4(string name, object defaultValue);
		Func<bool> AddUserVar_Bool(string name, bool defaultValue);
		Func<string> AddUserVar_String(string name, string defaultValue);
		Func<object> AddUserVar_Choice(string name, IEnumerable<object> choices, object defaultValue);
		#endregion

		void SetFrameCallback(FrameCallback callback);

		// Get access to the scene.
		// TODO: Strong typing.
		dynamic GetScene();

		// Write a string to the log.
		// (Unfortunately Roslyn scripting does not appear to let us redirect console output like IronPython does).
		void Log(string line);

		// Handles to special resources.
		IDepthBuffer DepthBuffer { get; }
		IDepthBuffer NoDepthBuffer { get; }

		IShaderResource BlackTexture { get; }
		IShaderResource WhiteTexture { get; }
		IShaderResource DefaultNormalTexture { get; }
	}
}
