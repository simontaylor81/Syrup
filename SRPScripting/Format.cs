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
		R8G8B8A8_UNorm_SRGB,
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

		private static FormatProperties[] formatProps = new[] 
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

/*
	// Component bit layout.
	public enum BitLayout
	{
		R8G8B8A8,
		R8,
		R16G16B16G16,
	}

	// Component data type
	public enum ComponentType
	{
		UNorm,
		SNorm,
		UInt,
		SInt,
		Float,
	}

	// Class that defines the format of an element in a resource (e.g. texture). Immutable.
	public class Format
	{
		public Format(BitLayout bitLayout, ComponentType componentType, bool srgb = false)
		{
			this.bitLayout = bitLayout;
			this.componentType = componentType;
			this.srgb = srgb;
		}

		// Size of an element of this format, in bytes.
		public int Size { get { return bitLayoutMetaData[(int)bitLayout].Size; } }

		// Number of components in this format.
		public int NumComponents { get { return bitLayoutMetaData[(int)bitLayout].NumComponents; } }

		// Easy accessors for some common use cases.
		public static Format RGBA32 { get { return _RGBA32; } }
		public static Format Float16RGBA { get { return _Float16RGBA; } }

		private static Format _RGBA32 = new Format(BitLayout.R8G8B8A8, ComponentType.UNorm);
		private static Format _Float16RGBA = new Format(BitLayout.R16G16B16G16, ComponentType.Float);

		public readonly BitLayout bitLayout;
		public readonly ComponentType componentType;
		public readonly bool srgb;

		// Meta-data about each bitlayout.
		private struct BitLayoutProperties
		{
			public int Size;
			public int NumComponents;

			public BitLayoutProperties(int size, int numComponents)
			{
				Size = size;
				NumComponents = numComponents;
			}
		}

		private static BitLayoutProperties[] bitLayoutMetaData = new[] 
			{
				new BitLayoutProperties(4, 4),	// R8G8B8A8
				new BitLayoutProperties(1, 1),	// R8
				new BitLayoutProperties(8, 4),	// R16G16B16G16
			};
	}
 */
}
