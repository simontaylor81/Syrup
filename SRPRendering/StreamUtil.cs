using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SRPCommon.Scripting;
using SRPScripting;

namespace SRPRendering
{
	// Helpers for filling SlimDX streams from dynamic (script) sources.
	static class StreamUtil
	{
		public static DataStream CreateStream1D(dynamic contents, int sizeX, Format format)
		{
			Func<int, int, int, dynamic> callbackAdapter = (x, y, z) => contents(x);
			return CreateStream(contents, sizeX, 1, 1, format, callbackAdapter);
		}
		public static DataStream CreateStream2D(dynamic contents, int sizeX, int sizeY, Format format)
		{
			Func<int, int, int, dynamic> callbackAdapter = (x, y, z) => contents(x, y);
			return CreateStream(contents, sizeX, sizeY, 1, format, callbackAdapter);
		}
		public static DataStream CreateStream3D(dynamic contents, int sizeX, int sizeY, int sizeZ, Format format)
		{
			Func<int, int, int, dynamic> callbackAdapter = (x, y, z) => contents(x, y, z);
			return CreateStream(contents, sizeX, sizeY, sizeZ, format, callbackAdapter);
		}

		// Create a SlimDX raw data stream based on the given dynamic object.
		private static DataStream CreateStream(dynamic contents, int sizeX, int sizeY, int sizeZ, Format format, Func<int, int, int, dynamic> callbackAdapter)
		{
			var stream = new DataStream(sizeX * sizeY * sizeZ * format.Size(), true, true);

			// For some reason it won't dynamically overload on the delegate type,
			// so we have to convert it by hand.
			// Python isn't picky about what parameters it expects, so we can just pass anything here.
			if (ScriptHelper.CanConvert<Func<dynamic>>(contents))
			{
				// Use the adapter instead of the actual value to convert from different dimensionalities.
				FillStream(callbackAdapter, stream, sizeX, sizeY, sizeZ, format);
			}
			else
			{
				FillStream(contents, stream, sizeX * sizeY * sizeZ, format);
			}

			// Reset position
			stream.Position = 0;
			return stream;
		}

		// Fill a data stream from a dynamic enumerable.
		private static void FillStream(IEnumerable<dynamic> enumerable, DataStream stream, int numElements, Format format)
		{
			foreach (var element in enumerable.Take(numElements))
			{
				WriteElement(stream, element, format);
			}
		}

		// Fill a data stream from a function taking the element index.
		private static void FillStream(Func<int, int, int, dynamic> fn, DataStream stream, int sizeX, int sizeY, int sizeZ, Format format)
		{
			for (int z = 0; z < sizeZ; z++)
			{
				for (int y = 0; y < sizeY; y++)
				{
					for (int x = 0; x < sizeX; x++)
					{
						var element = fn(x, y, z);
						WriteElement(stream, element, format);
					}
				}
			}
		}

		// Write a single element to the stream.
		private static void WriteElement(DataStream stream, dynamic element, Format format)
		{
			var numComponents = format.NumComponents();

			// If the element is a vector, coerce it to an array.
			element = ScriptHelper.CoerceVectorToArray(element);

			switch (format)
			{
				case Format.R32G32B32A32_Float:
				case Format.R32G32B32_Float:
					for (int i = 0; i < numComponents; i++)
						stream.Write((float)element[i]);
					break;

				case Format.R16G16B16A16_Float:
					for (int i = 0; i < numComponents; i++)
						stream.Write<Half>(new Half((float)element[i]));
					break;

				case Format.R16G16B16A16_UNorm:
					for (int i = 0; i < numComponents; i++)
						stream.Write((ushort)ToUNorm(element[i], 65535.0f));
					break;

				case Format.R8G8B8A8_UNorm:
				case Format.R8G8B8A8_UNorm_SRGB:
					for (int i = 0; i < numComponents; i++)
						stream.Write((byte)ToUNorm((float)element[i], 255.0f));
					break;

				case Format.R8G8B8A8_UInt:
					for (int i = 0; i < numComponents; i++)
						stream.Write<byte>(element[i]);
					break;

				//case Format.R8G8B8A8_SNorm:
				//	break;

				case Format.R8G8B8A8_SInt:
					for (int i = 0; i < numComponents; i++)
						stream.Write<sbyte>(element[i]);
					break;

				case Format.R32_Float:
					stream.Write((float)element);
					break;

				case Format.R16_Float:
					stream.Write<Half>(new Half((float)element));
					break;

				case Format.R8_UNorm:
					stream.Write((byte)ToUNorm((float)element, 255.0f));
					break;

				case Format.R8_UInt:
					stream.Write<byte>(element);
					break;

				//case Format.R8_SNorm:
				//	break;

				case Format.R8_SInt:
					stream.Write<sbyte>(element);
					break;

				default:
					throw new ScriptException("Unsuported format: " + format.ToString());
			}
		}

		private static uint ToUNorm(float value, float max)
		{
			return (uint)(Math.Max(0.0f, Math.Min(1.0f, value)) * max);
		}

		// Create a stream from an enumerable (directly, no format conversion).
		public static DataStream ToDataStream<T>(this IEnumerable<T> contents) where T : struct
		{
			var size = contents.Count() * Marshal.SizeOf(typeof(T));
			var result = new DataStream(size, true, true);
			foreach (var element in contents)
			{
				result.Write(element);
			}

			// Reset position
			result.Position = 0;
			return result;
		}
	}
}
