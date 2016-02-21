using System;
using System.Collections.Generic;

using SlimDX;
using SlimDX.Direct3D11;
using SRPCommon.Util;

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
			vertexBuffer = new SlimDX.Direct3D11.Buffer(device, vertexStream, (int)vertexStream.Length, ResourceUsage.Default,
				BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
			indexBuffer = new SlimDX.Direct3D11.Buffer(device, indexStream, (int)indexStream.Length, ResourceUsage.Default,
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
			context.InputAssembler.SetIndexBuffer(indexBuffer, SlimDX.DXGI.Format.R16_UInt, 0);
			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			context.DrawIndexed(numIndices, 0, 0);
		}

		private SlimDX.Direct3D11.Buffer vertexBuffer;
		private SlimDX.Direct3D11.Buffer indexBuffer;

		int numIndices;
		int vertexStride;

		public InputElement[] InputElements { get; }
	}
}
