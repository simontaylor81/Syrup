using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D11;
using SRPScripting;

namespace SRPRendering
{
	class Buffer : IBuffer, IDisposable
	{
		private SlimDX.Direct3D11.Buffer _buffer;

		public ShaderResourceView SRV { get; }
		public UnorderedAccessView UAV { get; }

		public Buffer(Device device, int sizeInBytes, bool uav)
		{
			var desc = new BufferDescription();

			desc.SizeInBytes = sizeInBytes;
			desc.Usage = ResourceUsage.Default;
			desc.CpuAccessFlags = CpuAccessFlags.Read;  // Need this for result verification.
			desc.OptionFlags = ResourceOptionFlags.StructuredBuffer;
			desc.StructureByteStride = sizeof(float);   // TODO

			desc.BindFlags = BindFlags.ShaderResource;
			if (uav)
			{
				desc.BindFlags |= BindFlags.UnorderedAccess;
			}

			_buffer = new SlimDX.Direct3D11.Buffer(device, desc);

			// Create SRV for reading from the buffer.
			SRV = new ShaderResourceView(device, _buffer);

			// Create UAV is required.
			if (uav)
			{
				UAV = new UnorderedAccessView(device, _buffer);
			}
		}

		public void Dispose()
		{
			UAV?.Dispose();
			SRV.Dispose();
			_buffer.Dispose();
		}

		// Read back the contents of the buffer from the GPU.
		public IEnumerable<T> GetContents<T>() where T : struct
		{
			var data = _buffer.Device.ImmediateContext.MapSubresource(_buffer, MapMode.Read, MapFlags.None);
			return data.Data.ReadRange<T>((int)(data.Data.Length / Marshal.SizeOf(typeof(T))));
		}
	}
}
