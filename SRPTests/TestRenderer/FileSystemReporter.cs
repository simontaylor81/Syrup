using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace SRPTests.TestRenderer
{
	// Test reporter that just writes out the generated images to disk so you can look at them.s
	class FileSystemReporter : ITestReporter
	{
		// Write results to temp directory.
		private readonly string _outDir = Path.Combine(Path.GetTempPath(), "SyrupTestOutput");

		public FileSystemReporter()
		{
			// Make sure the output directory exists.
			Directory.CreateDirectory(_outDir);
		}

		public Task TestCompleteAsync(string name, bool bSuccess, Bitmap result)
		{
			// Result may be null if we failed before even getting to the rendering.
			if (result != null)
			{
				// Save to output directory.
				result.Save(Path.Combine(_outDir, name + ".png"), ImageFormat.Png);
			}

			return Task.Delay(0);
		}

		public void Dispose()
		{
			// Write something to the log so the user knows where to find the images.
			Console.WriteLine($"Result images written to {_outDir}");
		}
	}
}
