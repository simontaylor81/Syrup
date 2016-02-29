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
		private static readonly string _expectedResultDir = Path.Combine(_baseDir, "ExpectedResults");
		private static readonly string _testDefinitionFile = Path.Combine(_baseDir, "tests.json");

		private static bool bLoggedDevice = false;

		public RenderTestHarness(TestReporter reporter)
		{
			_reporter = reporter;

			_renderer = new TestRenderer(64, 64);
			_workspace = new TestWorkspace(_baseDir);
			_scripting = new Scripting(_workspace);

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
		public async Task RenderScript(string name, Script script)
		{
			bool bSuccess = false;
			Bitmap result = null;

			try
			{
				// Execute the script.
				await _scripting.RunScript(script);

				// This should never fire, as the exception should propagate out of RunScript.
				Assert.False(_sr.HasScriptError, "Error executing script");

				// Render it.
				result = _renderer.Render(_sr);

				Assert.False(_sr.HasScriptError, "Error executing script render callback");

				// Load the image to compare against.
				var expectedImageFilename = Path.Combine(_expectedResultDir, name + ".png");
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

		public static IEnumerable<object[]> ScriptFiles => TestDefinitions.Load(_testDefinitionFile);
	}
}
