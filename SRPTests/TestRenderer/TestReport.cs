using SRPTests.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRPTests.TestRenderer
{
	public struct TestResult
	{
		public string name;
		public bool bSuccess;
		public Bitmap resultImage;
	}

	public class TestReport : IDisposable
	{
		private List<TestResult> results = new List<TestResult>();

		public void Dispose()
		{
			// Write the report on clean up.
			GenerateReport();

			// If we're running locally, write the result bitmaps to local directory for updating expected results.
			if (!CIHelper.IsCI)
			{
				var destDir = ".\\Results";
				if (!Directory.Exists(destDir))
				{
					Directory.CreateDirectory(destDir);
				}

				foreach (var result in results)
				{
					if (result.resultImage != null)
					{
						result.resultImage.Save(Path.Combine(destDir, result.name + ".png"), ImageFormat.Png);
					}
				}
			}
		}

		public void AddResult(TestResult result)
		{
			results.Add(result);
		}

		public void GenerateReport()
		{
			// Write to current working directory (usually the bin dir).
			var filename = ".\\SRPTestReport.html";

			using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
			{
				writer.WriteLine("<!DOCTYPE html>");
				writer.WriteLine("<html lang=\"en\">");
				writer.WriteLine("<head>");
				writer.WriteLine("<meta charset=\"utf-8\">");
				writer.WriteLine("<title>Test Report</title>");
				writer.WriteLine("</head>");
				writer.WriteLine("<body>");

				foreach (var result in results)
				{
					writeResult(result, writer);
				}

				writer.WriteLine("</body>");
				writer.WriteLine("</head>");
			}

			Console.WriteLine("Wrote test report to {0}", Path.GetFullPath(filename));

			// TODO: any way to make this async?
			CIHelper.PublishArtefact(filename).Wait();
		}

		private void writeResult(TestResult result, StreamWriter writer)
		{
			writer.WriteLine("<div>");
			writer.WriteLine("{0} - {1}", result.name, result.bSuccess ? "Success" : "Failure");

			if (result.resultImage != null)
			{
				writer.WriteLine("<img width=\"{0}\" height=\"{1}\" src=\"{2}\" />",
					result.resultImage.Width,
					result.resultImage.Height,
					ToBase64(result.resultImage));
			}

			writer.WriteLine("</div>");
		}

		private string ToBase64(Bitmap bitmap)
		{
			using (var stream = new MemoryStream())
			{
				bitmap.Save(stream, ImageFormat.Png);
				var base64 = Convert.ToBase64String(stream.GetBuffer());
				return "data:image/png;base64," + base64;
			}
		}
	}
}
