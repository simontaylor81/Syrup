using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SRPScripting;

namespace SRPRendering.Resources
{
	class Buffer : ID3DShaderResource, IDisposable
	{
		private SharpDX.Direct3D11.Buffer _buffer;

		public int ElementCount { get; }
		public int SizeInBytes { get; }

		public ShaderResourceView SRV { get; }
		public UnorderedAccessView UAV { get; }

		// Create a structured buffer with typed initial data, for when calling from C# directly.
		public static Buffer CreateStructured<T>(Device device, bool uav, IEnumerable<T> contents) where T : struct
		{
			using (var initialData = contents.ToDataStream())
			{
				return new Buffer(device, (int)initialData.Length, Marshal.SizeOf<T>(), uav, initialData);
			}
		}

		public Buffer(Device device, int sizeInBytes, int stride, bool uav, DataStream initialData)
		{
			var desc = new BufferDescription();

			desc.SizeInBytes = sizeInBytes;
			desc.Usage = ResourceUsage.Default;
			desc.CpuAccessFlags = CpuAccessFlags.Read;  // Need this for result verification.
			desc.OptionFlags = ResourceOptionFlags.BufferStructured;
			desc.StructureByteStride = stride;

			desc.BindFlags = BindFlags.ShaderResource;
			if (uav)
			{
				desc.BindFlags |= BindFlags.UnorderedAccess;
			}

			if (initialData != null)
			{
				_buffer = new SharpDX.Direct3D11.Buffer(device, initialData, desc);
			}
			else
			{
				// Passing null initialData to the Buffer constructor throws, so must use the other overload.
				_buffer = new SharpDX.Direct3D11.Buffer(device, desc);
			}

			// Create SRV for reading from the buffer.
			SRV = new ShaderResourceView(device, _buffer);

			// Create UAV is required.
			if (uav)
			{
				UAV = new UnorderedAccessView(device, _buffer);
			}
			
			ElementCount = sizeInBytes / stride;
			SizeInBytes = sizeInBytes;
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
			DataStream stream;
			_buffer.Device.ImmediateContext.MapSubresource(_buffer, 0, MapMode.Read, MapFlags.None, out stream);
			using (stream)
			{
				return stream.ReadRange<T>((int)(stream.Length / Marshal.SizeOf<T>()));
			}
		}
	}
}
