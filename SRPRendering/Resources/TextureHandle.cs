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

		public abstract void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator);
	}

	// Handle to a texture loaded from a file.
	class TextureHandleFile : TextureHandle
	{
		private readonly string _filename;
		private readonly MipGenerationMode _mipGenerationMode;
		private readonly string _mipGenerationShader;

		public TextureHandleFile(string filename, MipGenerationMode mipGenerationMode, string mipGenerationShader)
		{
			_filename = filename;
			_mipGenerationMode = mipGenerationMode;
			_mipGenerationShader = mipGenerationShader;
		}

		// Create the texture object.
		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Texture texture;
			try
			{
				texture = Texture.LoadFromFile(renderDevice.Device, _filename, _mipGenerationMode, logger);
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
			// TODO: Better interface for this.
			if (_mipGenerationMode == MipGenerationMode.CreateOnly)
			{
				// Generate custom mips.
				mipGenerator.Generate(texture, _mipGenerationShader);
			}

			Resource = texture;
		}
	}

	// Handle to a texture created procedurally by the script.
	class TextureHandleScript : TextureHandle
	{
		private readonly int _width;
		private readonly int _height;
		private readonly Format _format;
		private readonly object _contents;
		private readonly bool _generateMips;

		public TextureHandleScript(int width, int height, Format format, object contents, bool generateMips)
		{
			_width = width;
			_height = height;
			_format = format;
			_contents = contents;
			_generateMips = generateMips;
		}

		public override void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator)
		{
			Resource = Texture.CreateFromScript(renderDevice.Device, _width, _height, _format, _contents, _generateMips);
		}
	}
}
