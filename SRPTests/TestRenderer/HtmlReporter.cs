﻿using SRPTests.Util;
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

	public class HtmlReporter : ITestReporter
	{
		private List<TestResult> results = new List<TestResult>();

		public Task TestCompleteAsync(string name, bool bSuccess, Bitmap result)
		{
			// Add to list of results. All the interesting stuff happens at the end.
			results.Add(new TestResult { name = name, bSuccess = bSuccess, resultImage = result });
			return Task.Delay(0);
		}

		public void Dispose()
		{
			// Write the report on clean up.
			GenerateReport();
		}

		private void GenerateReport()
		{
			// Write to temp directory.
			var outDir = Path.Combine(Path.GetTempPath(), "SyrupTestOutput");
			Directory.CreateDirectory(outDir);
			var filename = Path.Combine(outDir, "SRPTestReport.html");

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
					WriteResult(result, writer);
				}

				writer.WriteLine("</body>");
				writer.WriteLine("</head>");
			}

			Console.WriteLine("Wrote test report to {0}", Path.GetFullPath(filename));

			// TODO: any way to make this async?
			CIHelper.PublishArtefact(filename).Wait();
		}

		private void WriteResult(TestResult result, StreamWriter writer)
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
