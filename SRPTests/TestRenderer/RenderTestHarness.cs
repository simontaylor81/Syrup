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
	public class RenderTestHarness : IDisposable, IClassFixture<FermiumReporter>
	{
		private readonly TestRenderer _renderer;
		private readonly TestWorkspace _workspace;
		private readonly ScriptRenderControl _src;
		private readonly Scripting _scripting;
		private readonly FermiumReporter _fermium;

		public RenderTestHarness(FermiumReporter fermium)
		{
			_fermium = fermium;

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
		public async Task RenderScript(string scriptFile)
		{
			bool bSuccess = false;
			Bitmap result = null;
			var name = Path.GetFileNameWithoutExtension(scriptFile);

			try
			{
				// Execute the script.
				await _scripting.RunScript(File.ReadAllText(scriptFile));

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
				// Report result to Fermium.
				await _fermium.TestComplete(name, bSuccess, BitmapToBytes(result));
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

		// Convert an image to PNG-encoded byte array.
		private byte[] BitmapToBytes(Bitmap bitmap)
		{
			byte[] result = null;
			if (bitmap != null)
			{
				using (var stream = new MemoryStream())
				{
					bitmap.Save(stream, ImageFormat.Png);
					result = stream.ToArray();
				}
			}

			return result;
		}
	}
}
