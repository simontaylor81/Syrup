using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Logging;
using SRPCommon.Scripting;
using SRPScripting;

namespace SRPRendering.Resources
{
	// Base class for handles to textures to give to the script.
	abstract class TextureHandle : ITexture2D, IDeferredResource
	{
		public ID3DShaderResource Resource { get; protected set; }

		protected bool? GenerateMips { get; private set; }
		protected string CustomMipShaderFilename { get; private set; }

		protected MipGenerationMode GetMipGenerationMode(bool generateMipsByDefault)
		{
			if (GenerateMips ?? generateMipsByDefault)
			{
				return MipGenerationMode.Full;
			}
			else if (CustomMipShaderFilename != null)
			{
				return MipGenerationMode.CreateOnly;
			}
			return MipGenerationMode.None;
		}

		public ITexture2D WithMips(bool generateMips = true)
		{
			if (generateMips && CustomMipShaderFilename != null)
			{
				throw new ScriptException("Textures cannot have custom and regular mips");
			}

			GenerateMips = generateMips;
			return this;
		}

		public ITexture2D WithCustomMips(string shaderFilename)
		{
			if (GenerateMips == true)
			{
				throw new ScriptException("Textures cannot have custom and regular mips");
			}

			CustomMipShaderFilename = shaderFilename;
			return this;
		}

		public abstract void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator);
	}

	// Handle to a texture loaded from a file.
	class TextureHandleFile : TextureHandle
	{
		private readonly string _filename;

		public TextureHandleFile(string filename)
		{
			_filename = filename;
		}

		// Create the texture object.
		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Texture texture;
			try
			{
				// Textures from a file generate mips by default.
				texture = Texture.LoadFromFile(renderDevice.Device, _filename, GetMipGenerationMode(true), logger);
			}
			catch (FileNotFoundException ex)
			{
				throw new ScriptException("Could not file texture file: " + _filename, ex);
			}
			catch (Exception ex)
			{
				throw new ScriptException("Error loading texture file: " + _filename, ex);
			}

			// We want mip generation errors to be reported directly, so this is
			// outside the above try-catch.
			if (CustomMipShaderFilename != null)
			{
				// Generate custom mips.
				mipGenerator.Generate(texture, CustomMipShaderFilename);
			}

			Resource = texture;
		}
	}

	// Handle to a texture created procedurally by the script from an enumerable.
	class TextureHandleEnumerable<T> : TextureHandle
	{
		private readonly int _width;
		private readonly int _height;
		private readonly Format _format;
		private readonly IEnumerable<T> _contents;

		public TextureHandleEnumerable(int width, int height, Format format, IEnumerable<T> contents)
		{
			_width = width;
			_height = height;
			_format = format;
			_contents = contents;
		}

		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			// TODO: Stronger typing here?
			using (var stream = StreamUtil.CreateStream(_contents.Cast<object>(), _width * _height, _format))
			{
				// Textures from script do not generate mips by default.
				Resource = Texture.CreateFromStream(renderDevice.Device, _width, _height, _format, stream, GetMipGenerationMode(false));
			}
		}
	}

	// Handle to a texture created procedurally by the script.
	class TextureHandleCallback : TextureHandle
	{
		private readonly int _width;
		private readonly int _height;
		private readonly Format _format;
		private readonly Func<int, int, object> _contentsCallback;

		public TextureHandleCallback(int width, int height, Format format, Func<int, int, object> contentsCallback)
		{
			_width = width;
			_height = height;
			_format = format;
			_contentsCallback = contentsCallback;
		}

		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			using (var stream = StreamUtil.CreateStream2D(_contentsCallback, _width, _height, _format))
			{
				// Textures from script do not generate mips by default.
				Resource = Texture.CreateFromStream(renderDevice.Device, _width, _height, _format, stream, GetMipGenerationMode(false));
			}
		}
	}

	// Handle to a texture created procedurally by the script.
	// TODO: Remove?
	class TextureHandleDynamic : TextureHandle
	{
		private readonly int _width;
		private readonly int _height;
		private readonly Format _format;
		private readonly object _contents;

		public TextureHandleDynamic(int width, int height, Format format, object contents)
		{
			_width = width;
			_height = height;
			_format = format;
			_contents = contents;
		}

		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			using (var stream = StreamUtil.CreateStream2DDynamic(_contents, _width, _height, _format))
			{
				// Textures from script do not generate mips by default.
				Resource = Texture.CreateFromStream(renderDevice.Device, _width, _height, _format, stream, GetMipGenerationMode(false));
			}
		}
	}
}
