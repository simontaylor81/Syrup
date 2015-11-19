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
using Xunit;

namespace SRPTests.TestRenderer
{
	public class RenderTestHarness : IDisposable, IClassFixture<TestReporter>
	{
		private readonly TestRenderer _renderer;
		private readonly TestWorkspace _workspace;
		private readonly SyrupRenderer _sr;
		private readonly Scripting _scripting;
		private readonly TestReporter _reporter;

		private static readonly string _baseDir = Path.Combine(GlobalConfig.BaseDir, @"SRPTests\TestScripts");

		public RenderTestHarness(TestReporter reporter)
		{
			_reporter = reporter;

			_renderer = new TestRenderer(64, 64);
			_workspace = new TestWorkspace(_baseDir);
			_scripting = new Scripting(_workspace);

			// Create syrup renderer to drive the rendering.
			_sr = new SyrupRenderer(_workspace, _renderer.Device, _scripting);
			_scripting.RenderInterface = _sr.ScriptInterface;
		}

		public void Dispose()
		{
			_renderer.Dispose();
			_sr.Dispose();
		}

		[Theory]
		[MemberData("ScriptFiles")]
		public async Task RenderScript(string scriptFile)
		{
			bool bSuccess = false;
			Bitmap result = null;
			var name = Path.GetFileNameWithoutExtension(scriptFile);

			try
			{
				// Execute the script.
				await _scripting.RunScript(new Script(scriptFile));

				Assert.False(_sr.HasScriptError, "Error executing script");

				// Render it.
				result = _renderer.Render(_sr);

				Assert.False(_sr.HasScriptError, "Error executing script render callback");

				// Load the image to compare against.
				var expectedImageFilename = Path.ChangeExtension(scriptFile, "png");
				Assert.True(File.Exists(expectedImageFilename), "No expected image to compare against.");
				var expected = new Bitmap(expectedImageFilename);

				// Compare the images.
				ImageComparison.AssertImagesEqual(expected, result);
				bSuccess = true;
			}
			finally
			{
				// Report result to Fermium.
				await _reporter.TestCompleteAsync(name, bSuccess, result);
			}
		}

		public static IEnumerable<object[]> ScriptFiles
		{
			get
			{
				return Directory.EnumerateFiles(_baseDir, "*.py")
					.Select(file => new[] { file });
			}
		}
	}
}
