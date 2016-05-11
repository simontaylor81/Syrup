using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Util;
using SRPScripting;
using SRPScripting.Shader;

namespace SRPCommon.Scripting
{
	// Wrapper around the statically-type render interface for dynamically-typed languages.
	class DynamicRenderInterfaceWrapper : IDynamicRenderInterface
	{
		private readonly IRenderInterface _typedInterface;

		public DynamicRenderInterfaceWrapper(IRenderInterface typedInterface)
		{
			_typedInterface = typedInterface;
		}

		public IShaderResource BlackTexture => _typedInterface.BlackTexture;
		public IShaderResource WhiteTexture => _typedInterface.WhiteTexture;
		public IShaderResource DefaultNormalTexture => _typedInterface.DefaultNormalTexture;

		public IDepthBuffer DepthBuffer => _typedInterface.DepthBuffer;
		public IDepthBuffer NoDepthBuffer => _typedInterface.NoDepthBuffer;

		public IShader CompileShader(string filename, string entryPoint, string profile, IDictionary<string, object> defines = null)
		{
			return _typedInterface.CompileShader(filename, entryPoint, profile, defines);
		}

		public IBuffer CreateBuffer(int numElements, Format format, dynamic contents)
		{
			IEnumerable<dynamic> contentsEnumerable;

			if (contents != null && ScriptHelper.CanConvert<Func<dynamic>>(contents))
			{
				// Convert callback to an enumerable for uniform handling.
				contentsEnumerable = Enumerable.Range(0, numElements).Select(x => contents(x));
			}
			else
			{
				contentsEnumerable = contents;
			}

			return _typedInterface.CreateTypedBuffer(numElements, format, contentsEnumerable);
		}

		public IRenderTarget CreateRenderTarget() => _typedInterface.CreateRenderTarget();

		public ITexture2D CreateTexture2D(int width, int height, Format format, dynamic contents)
		{
			IEnumerable<dynamic> contentsEnumerable;

			if (ScriptHelper.CanConvert<Func<dynamic>>(contents))
			{
				// Convert callback to an enumerable for uniform handling.
				contentsEnumerable = EnumerableUtil.Range2D(width, height, (x, y) => contents(x, y));
			}
			else
			{
				contentsEnumerable = contents;
			}

			return _typedInterface.CreateTexture2D(width, height, format, contentsEnumerable);
		}

		public dynamic GetScene() => _typedInterface.GetScene();

		public ITexture2D LoadTexture(string path) => _typedInterface.LoadTexture(path);

		public void Log(string line) => _typedInterface.Log(line);

		public void SetFrameCallback(FrameCallback callback)
		{
			_typedInterface.SetFrameCallback(callback);
		}

		#region User Variables
		public dynamic AddUserVar_Float(string name, float defaultValue) => _typedInterface.AddUserVar_Float(name, defaultValue);
		public dynamic AddUserVar_Float2(string name, object defaultValue) => _typedInterface.AddUserVar_Float2(name, defaultValue);
		public dynamic AddUserVar_Float3(string name, object defaultValue) => _typedInterface.AddUserVar_Float3(name, defaultValue);
		public dynamic AddUserVar_Float4(string name, object defaultValue) => _typedInterface.AddUserVar_Float4(name, defaultValue);
		public dynamic AddUserVar_Int(string name, int defaultValue) => _typedInterface.AddUserVar_Int(name, defaultValue);
		public dynamic AddUserVar_Int2(string name, object defaultValue) => _typedInterface.AddUserVar_Int2(name, defaultValue);
		public dynamic AddUserVar_Int3(string name, object defaultValue) => _typedInterface.AddUserVar_Int3(name, defaultValue);
		public dynamic AddUserVar_Int4(string name, object defaultValue) => _typedInterface.AddUserVar_Int4(name, defaultValue);
		public dynamic AddUserVar_Bool(string name, bool defaultValue) => _typedInterface.AddUserVar_Bool(name, defaultValue);
		public dynamic AddUserVar_String(string name, string defaultValue) => _typedInterface.AddUserVar_String(name, defaultValue);

		public dynamic AddUserVar_Choice(string name, IEnumerable<object> choices, object defaultValue)
			=> _typedInterface.AddUserVar_Choice(name, choices.Cast<string>(), (string)defaultValue);
		#endregion
	}
}
