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
	public class RenderTestHarness : IDisposable, IClassFixture<TestReport>
	{
		private readonly TestRenderer _renderer;
		private readonly TestWorkspace _workspace;
		private readonly ScriptRenderControl _src;
		private readonly Scripting _scripting;
		private readonly TestReport _testReport;

		public RenderTestHarness(TestReport testReport)
		{
			_testReport = testReport;

			_renderer = new TestRenderer(64, 64);
			_workspace = new TestWorkspace();
			_scripting = new Scripting(_workspace);

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
		public async void RenderScript(string scriptFile)
		{
			bool bSuccess = false;
			Bitmap result = null;
			try
			{
				// Execute the script.
				await _scripting.RunScriptFromFile(scriptFile);

				Assert.False(_src.HasScriptError, "Error executing script");

				// Render it.
				result = _renderer.Render(_src);

				Assert.False(_src.HasScriptError, "Error executing script render callback");

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
				// Add result to test report.
				_testReport.AddResult(new TestResult()
				{
					name = Path.GetFileNameWithoutExtension(scriptFile),
					bSuccess = bSuccess,
					resultImage = result
				});
			}
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
