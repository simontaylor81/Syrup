using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Logging;
using SRPScripting;

namespace SRPRendering.Resources
{
	// Handle to a default texture (e.g. the white texture).
	class DefaultTextureHandle : ITexture2D, IDeferredResource
	{
		public ID3DShaderResource Resource { get; }

		public DefaultTextureHandle(ID3DShaderResource resource)
		{
			Resource = resource;
		}

		// Nothing to do -- default resources are created at startup.
		public void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator) { }
	}
}
