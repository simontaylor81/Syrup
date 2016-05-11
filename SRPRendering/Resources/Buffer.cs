using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SRPScripting;
using SharpDX.Direct3D;

namespace SRPRendering.Resources
{
	class Buffer : ID3DShaderResource, IDisposable
	{
		public SharpDX.Direct3D11.Buffer RawBuffer { get; }

		public int ElementCount { get; }
		public int SizeInBytes { get; }

		public ShaderResourceView SRV { get; }
		public UnorderedAccessView UAV { get; }

		// Create a structured buffer with typed initial data, for when calling from C# directly.
		public static Buffer CreateStructured<T>(Device device, bool uav, bool containsDrawIndirectArgs, IEnumerable<T> contents) where T : struct
		{
			using (var initialData = contents.ToDataStream())
			{
				return new Buffer(device, (int)initialData.Length, Marshal.SizeOf<T>(), uav, containsDrawIndirectArgs, initialData);
			}
		}

		public Buffer(Device device, int sizeInBytes, int stride, bool uav, bool containsDrawIndirectArgs, DataStream initialData)
		{
			// For now, create raw buffer for indirect args, otherwise create a structured buffer.
			// TODO: Explicit API for creating structured, raw and typed buffers.
			bool raw = containsDrawIndirectArgs;

			var desc = new BufferDescription();

			desc.SizeInBytes = sizeInBytes;
			desc.Usage = ResourceUsage.Default;
			desc.StructureByteStride = stride;

			// Add CPU read back for GetContents.
			// Annoying not possible for draw indirect args buffers.
			desc.CpuAccessFlags = containsDrawIndirectArgs ? 0 : CpuAccessFlags.Read;

			desc.BindFlags = BindFlags.ShaderResource;
			if (uav)
			{
				desc.BindFlags |= BindFlags.UnorderedAccess;
			}

			desc.OptionFlags = raw ? ResourceOptionFlags.BufferAllowRawViews : ResourceOptionFlags.BufferStructured;
			if (containsDrawIndirectArgs)
			{
				desc.OptionFlags |= ResourceOptionFlags.DrawIndirectArguments;
			}

			if (initialData != null)
			{
				RawBuffer = new SharpDX.Direct3D11.Buffer(device, initialData, desc);
			}
			else
			{
				// Passing null initialData to the Buffer constructor throws, so must use the other overload.
				RawBuffer = new SharpDX.Direct3D11.Buffer(device, desc);
			}

			// Create SRV for reading from the buffer.
			if (raw)
			{
				var srvDesc = new ShaderResourceViewDescription();
				srvDesc.Dimension = ShaderResourceViewDimension.ExtendedBuffer;
				srvDesc.BufferEx.Flags = raw ? ShaderResourceViewExtendedBufferFlags.Raw : ShaderResourceViewExtendedBufferFlags.None;
				srvDesc.BufferEx.FirstElement = 0;
				srvDesc.BufferEx.ElementCount = sizeInBytes / 4;
				srvDesc.Format = SharpDX.DXGI.Format.R32_Typeless;
				SRV = new ShaderResourceView(device, RawBuffer, srvDesc);
			}
			else
			{
				SRV = new ShaderResourceView(device, RawBuffer);
			}

			// Create UAV if required.
			if (uav)
			{
				UAV = new UnorderedAccessView(device, RawBuffer);
			}

			ElementCount = sizeInBytes / stride;
			SizeInBytes = sizeInBytes;
		}

		public void Dispose()
		{
			UAV?.Dispose();
			SRV.Dispose();
			RawBuffer.Dispose();
		}

		// Read back the contents of the buffer from the GPU.
		public IEnumerable<T> GetContents<T>() where T : struct
		{
			DataStream stream;
			RawBuffer.Device.ImmediateContext.MapSubresource(RawBuffer, 0, MapMode.Read, MapFlags.None, out stream);
			using (stream)
			{
				return stream.ReadRange<T>((int)(stream.Length / Marshal.SizeOf<T>()));
			}
		}
	}
}
