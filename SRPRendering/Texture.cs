using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectXTexSlim;
using SlimDX.Direct3D11;
using SlimDX;
using SRPScripting;
using SRPCommon.Util;
using SRPCommon.Scripting;

namespace SRPRendering
{
	public class Texture : IDisposable
	{
		public Texture2D Texture2D { get; }
		public ShaderResourceView SRV { get; }

		// Simple constructor taking a texture and shader resource view.
		public Texture(Texture2D texture2D, ShaderResourceView srv)
		{
			Texture2D = texture2D;
			SRV = srv;
		}

		public void Dispose()
		{
			DisposableUtil.SafeDispose(SRV);
			DisposableUtil.SafeDispose(Texture2D);
		}

		// Create a texture from a file.
		// TODO: Not sure if this is the best strategy long term.
		// Probably want to separate import from render resource creation.
		public static Texture LoadFromFile(Device device, string filename)
		{
			try
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();

				// Load the texture itself using DirectXTex.
				var image = LoadImage(filename);
				var texture2D = image.CreateTexture(device);

				stopwatch.Stop();
				Console.WriteLine("Loading {0} took {1} ms.", System.IO.Path.GetFileName(filename), stopwatch.ElapsedMilliseconds);

				// Create the SRV.
				var srv = new ShaderResourceView(device, texture2D);

				return new Texture(texture2D, srv);
			}
			catch (Exception ex)
			{
				// TODO: Better error handling.
				OutputLogger.Instance.LogLine(LogCategory.Log, "Failed to load texture file {0} Error code: 0x{1:x8}", filename, ex.HResult);
				throw;
			}
		}

		private static ScratchImage LoadImage(string filename)
		{
			var ext = Path.GetExtension(filename).ToLowerInvariant();
			if (ext == ".dds")
			{
				// Load .dds files using DDS loader.
				return DirectXTex.LoadFromDDSFile(filename);
			}
			else if (ext == ".tga")
			{
				// Load .tga files using TGA loader.
				return DirectXTex.LoadFromTGAFile(filename);
			}
			else
			{
				// Attempt to load all other images using WIC.
				return DirectXTex.LoadFromWICFile(filename);
			}
		}

		// Create a texture with data from script.
		public static Texture CreateFromScript(Device device, int width, int height, Format format, dynamic contents)
		{
			// Build texture descriptor.
			var desc = new Texture2DDescription()
				{
					Width = width,
					Height = height,
					Format = format.ToDXGI(),
					MipLevels = 1,		// TODO: generate mips?
					ArraySize = 1,
					BindFlags = BindFlags.ShaderResource,
					SampleDescription = new SlimDX.DXGI.SampleDescription(1, 0)
				};

			// Construct data stream from script data.
			var initialData = new DataRectangle(width * format.Size(), GetStreamFromDynamic(contents, width, height, format));

			// Create the actual texture resource.
			var texture2D = new Texture2D(device, desc, initialData);

			// Create the SRV.
			var srv = new ShaderResourceView(device, texture2D);

			return new Texture(texture2D, srv);
		}

		private delegate dynamic TexelCallback(int x, int y);

		// Create a SlimDX raw data stream based on the given dynamic object.
		private static DataStream GetStreamFromDynamic(dynamic contents, int width, int height, Format format)
		{
			var stream = new DataStream(width * height * format.Size(), true, true);

			// For some reason it won't dynamically overload on the delegate type,
			// so we have to convert it by hand.
			TexelCallback callback;
			if (ScriptHelper.Instance.Operations.TryConvertTo<TexelCallback>(contents, out callback))
			{
				FillStream(callback, stream, width, height, format);
			}
			else
			{
				FillStream(contents, stream, width, height, format);
			}

			// Reset position
			stream.Position = 0;
			return stream;
		}

		// Fill a data stream from a dynamic enumerable.
		private static void FillStream(IEnumerable<dynamic> enumerable, DataStream stream, int width, int height, Format format)
		{
			foreach (var element in enumerable.Take(width * height))
			{
				WriteDynamic(stream, element, format);
			}
		}

		// Fill a data stream from a function taking x & y coordinates.
		private static void FillStream(TexelCallback fn, DataStream stream, int width, int height, Format format)
		{
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					var element = fn(x, y);
					WriteDynamic(stream, element, format);
				}
			}
		}

		private static void WriteDynamic(DataStream stream, dynamic element, Format format)
		{
			switch (format)
			{
				case Format.R32G32B32A32_Float:
				case Format.R32G32B32_Float:
					for (int i = 0; i < format.NumComponents(); i++)
						stream.Write((float)element[i]);
					break;

				case Format.R16G16B16A16_Float:
					for (int i = 0; i < format.NumComponents(); i++)
						stream.Write<Half>(new Half((float)element[i]));
					break;

				case Format.R16G16B16A16_UNorm:
					for (int i = 0; i < format.NumComponents(); i++)
						stream.Write((ushort)ToUNorm(element[i], 65535.0f));
					break;

				case Format.R8G8B8A8_UNorm:
				case Format.R8G8B8A8_UNorm_SRGB:
					for (int i = 0; i < format.NumComponents(); i++)
						stream.Write((byte)ToUNorm((float)element[i], 255.0f));
					break;

				case Format.R8G8B8A8_UInt:
					for (int i = 0; i < format.NumComponents(); i++)
						stream.Write<byte>(element[i]);
					break;

				//case Format.R8G8B8A8_SNorm:
				//	break;

				case Format.R8G8B8A8_SInt:
					for (int i = 0; i < format.NumComponents(); i++)
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
	}
}
