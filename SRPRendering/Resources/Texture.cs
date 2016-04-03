﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SharpDX;
using SRPScripting;
using SRPCommon.Util;
using DirectXTexNet;
using SRPCommon.Logging;

namespace SRPRendering.Resources
{
	public enum MipGenerationMode
	{
		None,		// Create texture without mipchain
		Full,		// Generate full mipchain
		CreateOnly,	// Create the mip chain, but don't put any data in it.
	}

	public class Texture : ID3DShaderResource, IDisposable
	{
		public int Width { get; }
		public int Height { get; }

		public Texture2D Texture2D { get; }
		public ShaderResourceView SRV { get; }

		public UnorderedAccessView UAV { get { throw new NotImplementedException("TODO: Texture UAVs"); } }

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
		public static Texture LoadFromFile(Device device, string filename, MipGenerationMode mipGenerationMode, ILogger logger)
		{
			try
			{
				//var stopwatch = System.Diagnostics.Stopwatch.StartNew();

				// Load the texture itself using DirectXTex.
				var image = LoadImage(filename);
				CreateMipChain(image, mipGenerationMode);

				var texture2D = new Texture2D(image.CreateTexture(device.NativePointer));

				//stopwatch.Stop();
				//Console.WriteLine("Loading {0} took {1} ms.", System.IO.Path.GetFileName(filename), stopwatch.ElapsedMilliseconds);

				// Create the SRV.
				var srv = new ShaderResourceView(device, texture2D);

				return new Texture(texture2D, srv);
			}
			catch (Exception ex)
			{
				// TODO: Better error handling.
				logger.LogLine("Failed to load texture file {0} Error code: 0x{1:x8}", filename, ex.HResult);
				throw;
			}
		}

		private static IScratchImage LoadImage(string filename)
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
		public static Texture CreateFromStream(
			Device device, int width, int height, Format format, DataStream contents, MipGenerationMode mipGenerationMode)
		{
			// Construct data stream from script data.
			var initialData = new DataRectangle(contents.DataPointer, width * format.Size());

			// Create DirectXTex representation (so we can apply the same operations as images loaded
			// from disk, e.g. mip generation).
			var image = DirectXTex.Create2D(initialData.DataPointer, initialData.Pitch, width, height, (uint)format.ToDXGI());
			CreateMipChain(image, mipGenerationMode);

			// Create the actual texture resource.
			var texture2D = new Texture2D(image.CreateTexture(device.NativePointer));

			// Create the SRV.
			var srv = new ShaderResourceView(device, texture2D);

			return new Texture(texture2D, srv);
		}

		private static void CreateMipChain(IScratchImage image, MipGenerationMode mode)
		{
			if (mode == MipGenerationMode.Full)
			{
				image.GenerateMipMaps();
			}
			else if (mode == MipGenerationMode.CreateOnly)
			{
				image.CreateEmptyMipChain();
			}
		}

		private delegate dynamic TexelCallback(int x, int y);
	}
}
