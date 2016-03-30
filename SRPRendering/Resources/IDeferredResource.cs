using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Logging;

namespace SRPRendering.Resources
{
	// Interface for all deferred-creation resources.
	interface IDeferredResource
	{
		ID3DShaderResource Resource { get; }
		void CreateResource(RenderDevice renderDevice, ILogger logger, MipGenerator mipGenerator);
	}
}
