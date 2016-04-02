using SharpDX.Direct3D11;
using SRPScripting;

namespace SRPRendering.Resources
{
	class UavHandle : IUav
	{
		public UnorderedAccessView UAV { get; set; }
	}
}