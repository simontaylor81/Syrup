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
				Console.WriteLine($"RenderTestHarness: Using device '{_renderer.Device.Adapter.ToString()}'");
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
			// This should never fire, as the exception should propagate out of RunScript.
			Assert.That(_sr.HasScriptError, Is.False, "Error executing script");

			// Render stuff and return the resulting image.
			return _renderer.Render(_sr);
		}

		public void Dispatch()
		{
			// This should never fire, as the exception should propagate out of RunScript.
			Assert.That(_sr.HasScriptError, Is.False, "Error executing script");

			// Run the renderer to trigger compute shaders.
			_renderer.Dispatch(_sr);
		}
	}
}
