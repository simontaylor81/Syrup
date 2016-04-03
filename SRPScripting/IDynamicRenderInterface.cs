using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPScripting.Shader;

namespace SRPScripting
{
	// Interface to the rendering system exposed to dynamically-typed scripting languages (i.e. Python).
	public interface IDynamicRenderInterface
	{
		IShader CompileShader(string filename, string entryPoint, string profile,
			IDictionary<string, object> defines = null);

		// Create a render target of dimensions equal to the viewport.
		IRenderTarget CreateRenderTarget();

		// Create a 2D texture of the given size and format, and fill it with the given data.
		ITexture2D CreateTexture2D(int width, int height, Format format, dynamic contents);

		// Create a buffer of the given size and format, and fill it with the given data.
		IBuffer CreateBuffer(int numElements, Format format, dynamic contents);

		// Load a texture from a file.
		ITexture2D LoadTexture(string path);

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

		void SetFrameCallback(FrameCallback callback);

		// Get access to the scene.
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
