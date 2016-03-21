using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SRPScripting;

namespace SRPRendering.Resources
{
	interface ID3DShaderResource : IShaderResource
	{
		ShaderResourceView SRV { get; }
		UnorderedAccessView UAV { get; }
	}
}
