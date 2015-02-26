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
	public class TestReport : IDisposable
	{
		private List<Bitmap> results = new List<Bitmap>();

		public void Dispose()
		{
			// Write the report on clean up.
			GenerateReport();
		}

		public void AddResult(Bitmap bitmap)
		{
			results.Add(bitmap);
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

			// TODO: any way to make this async?
			CIHelper.PublishArtefact(filename).Wait();
		}

		private void writeResult(Bitmap result, StreamWriter writer)
		{
			writer.WriteLine("<img width=\"{0}\" height=\"{1}\" src=\"{2}\" />",
				result.Width,
				result.Height,
				ToBase64(result));
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
