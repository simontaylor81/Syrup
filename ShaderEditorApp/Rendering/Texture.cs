using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectXTexSlim;
using SlimDX.Direct3D11;

namespace ShaderEditorApp.Rendering
{
	class Texture : IDisposable
	{
		public Texture2D Texture2D { get; private set; }
		public ShaderResourceView SRV { get; private set; }

		// Simple constructor taking a texture and shader resource view.
		public Texture(Texture2D texture2D, ShaderResourceView srv)
		{
			Texture2D = texture2D;
			SRV = srv;
		}

		public void Dispose()
		{
			RenderUtils.SafeDispose(SRV);
			RenderUtils.SafeDispose(Texture2D);
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
	}
}
