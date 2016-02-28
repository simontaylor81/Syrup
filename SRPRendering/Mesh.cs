using System;
using System.Collections.Generic;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SRPCommon.Util;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace SRPRendering
{
	// Generic mesh class, containing vertex buffer, index buffer, etc.
	public class Mesh : IDrawable, IDisposable
	{
		public Mesh(Device device, DataStream vertexStream, int vertexStride, DataStream indexStream, InputElement[] inputElements)
		{
			// Make sure read/write pointers of the streams are reset.
			vertexStream.Position = 0;
			indexStream.Position = 0;

			// Create buffers.
			vertexBuffer = new Buffer(device, vertexStream, (int)vertexStream.Length, ResourceUsage.Default,
				BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
			indexBuffer = new Buffer(device, indexStream, (int)indexStream.Length, ResourceUsage.Default,
				BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			InputElements = inputElements;
			numIndices = (int)indexStream.Length / 2;	// Only support 16-bit indices.
			this.vertexStride = vertexStride;
		}

		public void Dispose()
		{
			DisposableUtil.SafeDispose(vertexBuffer);
			DisposableUtil.SafeDispose(indexBuffer);
		}

		public void Draw(DeviceContext context)
		{
			context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, vertexStride, 0));
			context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			context.DrawIndexed(numIndices, 0, 0);
		}

		private Buffer vertexBuffer;
		private Buffer indexBuffer;

		int numIndices;
		int vertexStride;

		public InputElement[] InputElements { get; }
	}
}
