using SRPCommon.Scripting;
using SRPCommon.Util;
using SRPRendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace SRPTests.TestRenderer
{
	public class RenderTestHarness : IDisposable
	{
		private readonly TestRenderer _renderer;
		private readonly TestWorkspace _workspace;
		private readonly ScriptRenderControl _src;
		private readonly Scripting _scripting;

		public RenderTestHarness()
		{
			// Set config for test environment (is there a better place/way of doing this?)
			_renderer = new TestRenderer(256, 256);
			_workspace = new TestWorkspace();
			_scripting = new Scripting();

			// Create script render control to drive the rendering.
			_src = new ScriptRenderControl(_workspace, _renderer.Device, _scripting);
			_scripting.RenderInterface = _src.ScriptInterface;
		}

		public void Dispose()
		{
			_renderer.Dispose();
			_src.Dispose();
		}

		[Theory]
		[MemberData("ScriptFiles")]
		public async void TestTest(string scriptFile)
		{
			// Execute the script.
			await _scripting.RunScriptFromFile(scriptFile);

			// Render it.
			var result = _renderer.Render(_src);

			// Load the image to compare against.
			var expectedImageFilename = Path.ChangeExtension(scriptFile, "png");
			Assert.True(File.Exists(expectedImageFilename), "No expected image to compare against.");
			var expected = new Bitmap(expectedImageFilename);

			// Compare the images.
			ImageComparison.AssertImagesEqual(expected, result);
		}

		public static IEnumerable<object[]> ScriptFiles
		{
			get
			{
				var directory = Path.Combine(GlobalConfig.BaseDir, @"SRPTests\TestScripts");
				return Directory.EnumerateFiles(directory, "*.py")
					.Select(file => new[] { file });
			}
		}
	}
}
