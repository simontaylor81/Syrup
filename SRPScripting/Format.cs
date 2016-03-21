using System;

namespace SRPScripting
{
	// TODO: Add more.
	public enum Format
	{
		R32G32B32A32_Float,
		R32G32B32_Float,
		R16G16B16A16_Float,
		R16G16B16A16_UNorm,
		R8G8B8A8_UNorm,
		R8G8B8A8_UNorm_SRgb,
		R8G8B8A8_UInt,
		R8G8B8A8_SNorm,
		R8G8B8A8_SInt,
		R32_Float,
		R16_Float,
		R8_UNorm,
		R8_UInt,
		R8_SNorm,
		R8_SInt,
	}

	// Format helper methods
	public static class FormatExtensions
	{
		// Size of an element of this format, in bytes.
		public static int Size(this Format format)
		{
			return formatProps[(int)format].Size;
		}

		// Number of components in this format.
		public static int NumComponents(this Format format)
		{
			return formatProps[(int)format].NumComponents;
		}

		// Meta-data about each foramt.
		private struct FormatProperties
		{
			public int Size;	// In bytes
			public int NumComponents;

			public FormatProperties(int size, int numComponents)
			{
				Size = size;
				NumComponents = numComponents;
			}
		}

		private static FormatProperties[] formatProps =
			{
				new FormatProperties(16, 4),	// R32G32B32A32_Float
				new FormatProperties(12, 3),	// R32G32B32_Float
				new FormatProperties(8,  4),	// R16G16B16A16_Float
				new FormatProperties(8,  4),	// R16G16B16A16_UNorm
				new FormatProperties(4,  4),	// R8G8B8A8_UNorm
				new FormatProperties(4,  4),	// R8G8B8A8_UNorm_SRGB
				new FormatProperties(4,  4),	// R8G8B8A8_UInt
				new FormatProperties(4,  4),	// R8G8B8A8_SNorm
				new FormatProperties(4,  4),	// R8G8B8A8_SInt
				new FormatProperties(4,  1),	// R32_Float
				new FormatProperties(2,  1),	// R16_Float
				new FormatProperties(1,  1),	// R8_UNorm
				new FormatProperties(1,  1),	// R8_UInt
				new FormatProperties(1,  1),	// R8_SNorm
				new FormatProperties(1,  1),	// R8_SInt
			};
	}
}
