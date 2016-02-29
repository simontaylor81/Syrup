using SRPCommon.Scripting;
using SRPCommon.Util;
using SRPRendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SRPScripting;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace ShaderUnit.TestRenderer
{
	public class RenderTestHarness : IDisposable
	{
		private readonly TestRenderer _renderer;
		private readonly TestWorkspace _workspace;
		private readonly SyrupRenderer _sr;

		private static readonly string _baseDir = Path.Combine(GlobalConfig.BaseDir, @"ShaderUnit\TestScripts");

		private static bool bLoggedDevice = false;

		public IRenderInterface RenderInterface => _sr.ScriptInterface;

		public RenderTestHarness()
		{
			_renderer = new TestRenderer(64, 64);
			_workspace = new TestWorkspace(_baseDir);

			// Minor hack to avoid spamming the log with device names.
			if (!bLoggedDevice)
			{
				// Write adapter description to the console, since it can affect results.
				// Trim weird null characters that appear from somewhere.
				var deviceName = _renderer.Device.Adapter.Description.Description.Trim('\0');
				Console.WriteLine($"RenderTestHarness: Using device '{deviceName}'");
				bLoggedDevice = true;
			}

			// Create syrup renderer to drive the rendering.
			_sr = new SyrupRenderer(_workspace, _renderer.Device, null);
		}

		public void Dispose()
		{
			_renderer.Dispose();
			_sr.Dispose();
		}

		public Bitmap RenderImage()
		{
			// Render stuff and return the resulting image.
			return _renderer.Render(_sr);
		}

		// Helper for the common case of rendering a fullscreen quad.
		public Bitmap RenderFullscreenImage(object vs, object ps)
		{
			RenderInterface.SetFrameCallback(context =>
			{
				context.DrawFullscreenQuad(vs, ps);
			});

			return RenderImage();
		}

		public void Dispatch()
		{
			// Run the renderer to trigger compute shaders.
			_renderer.Dispatch(_sr);
		}

		// Simple wrapper for the common 1D case.
		public IEnumerable<T> DispatchToBuffer<T>(object cs, string outBufferVariable, int size, int shaderNumThreads) where T : struct =>
			DispatchToBuffer<T>(cs, outBufferVariable, Tuple.Create(size, 1, 1), Tuple.Create(shaderNumThreads, 1, 1));

		public IEnumerable<T> DispatchToBuffer<T>(object cs, string outBufferVariable, Tuple<int, int, int> size, Tuple<int, int, int> shaderNumThreads) where T : struct
		{
			// Create buffer to hold results.
			var numElements = size.Item1 * size.Item2 * size.Item3;
			var outputBuffer = RenderInterface.CreateBuffer(numElements * Marshal.SizeOf(typeof(T)), Format.R32_Float, null, uav: true);
			RenderInterface.SetShaderUavVariable(cs, outBufferVariable, outputBuffer);

			int numThreadGroupsX = DivideCeil(size.Item1, shaderNumThreads.Item1);
			int numThreadGroupsY = DivideCeil(size.Item2, shaderNumThreads.Item2);
			int numThreadGroupsZ = DivideCeil(size.Item3, shaderNumThreads.Item3);

			RenderInterface.SetFrameCallback(context =>
			{
				context.Dispatch(cs, numThreadGroupsX, numThreadGroupsY, numThreadGroupsY);
			});

			// Render a frame to dispatch the compute shader.
			Dispatch();

			// Read results back from the buffer
			return outputBuffer.GetContents<T>();
		}

		// Integer division with round up instead of down.
		private int DivideCeil(int x, int multipleOf) => (x + multipleOf - 1) / multipleOf;

		public T ExecuteShaderFunction<T>(string shaderFile, string function, params object[] parameters) where T : struct
		{
			string shader = HlslTestHarness.GenerateComputeShader(shaderFile, function, typeof(T), parameters);
			var cs = RenderInterface.CompileShaderFromString(shader, HlslTestHarness.EntryPoint, "cs_5_0");

			return DispatchToBuffer<T>(cs, HlslTestHarness.OutBufferName, 1, 1).Single();
		}
	}
}
