using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ShaderUnit.TestRenderer
{
	public static class AssertEx
	{
		// Assert two bitmaps are binary identical.
		public static void ImagesEqual(Bitmap expected, Bitmap actual)
		{
			Assert.That(actual.Width, Is.EqualTo(expected.Width));
			Assert.That(actual.Height, Is.EqualTo(expected.Height));
			Assert.That(actual.PixelFormat, Is.EqualTo(expected.PixelFormat));

			// Check pixels one by one.
			for (int y = 0; y < expected.Height; y++)
			{
				for (int x = 0; x < expected.Width; x++)
				{
					AssertPixelsEqual(expected.GetPixel(x, y), actual.GetPixel(x, y), x, y);
				}
			}
		}

		// Custom equality assertion to allow the failing pixel to be reported.
		private static void AssertPixelsEqual(Color expected, Color actual, int x, int y)
		{
			// This is pretty hacky, but GPU variations mean that the results can
			// come out slightly differently on different hardware.
			if (!EqualTolerance(expected.R, actual.R, 1) ||
				!EqualTolerance(expected.G, actual.G, 1) ||
				!EqualTolerance(expected.B, actual.B, 1) ||
				!EqualTolerance(expected.A, actual.A, 1))
			{
				// TODO
				//throw new PixelEqualException(expected, actual, x, y);
				throw new Exception("Image mismatch");
			}
		}

		private static bool EqualTolerance(int a, int b, int tolerance) => Math.Abs(a - b) <= tolerance;

		// Special equality exception to allow us to report which pixel failed.
		//class PixelEqualException : AssertActualExpectedException
		//{
		//	public PixelEqualException(Color expected, Color actual, int x, int y)
		//		: base(expected, actual, string.Format("Pixel mismatch at ({0}, {1})", x, y))
		//	{
		//	}
		//}
	}
}
