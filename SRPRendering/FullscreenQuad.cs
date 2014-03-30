using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;

namespace SRPRendering
{
	class FullscreenQuad : IDisposable
	{
		public FullscreenQuad(Device device)
		{
			int vertexBufferSize = 4 * VertexStride;
			var vertexStream = new DataStream(vertexBufferSize, true, true);

			// Add the four quad verts to the stream.
			vertexStream.Write(new Vector4(-1.0f, -1.0f, 0.0f, 1.0f));
			vertexStream.Write(new Vector4(-1.0f,  1.0f, 0.0f, 1.0f));
			vertexStream.Write(new Vector4( 1.0f, -1.0f, 0.0f, 1.0f));
			vertexStream.Write(new Vector4( 1.0f,  1.0f, 0.0f, 1.0f));

			// Reset stream to the start.
			vertexStream.Position = 0;

			// Create the vertex buffer.
			vertexBuffer = new Buffer(device, vertexStream, vertexBufferSize,
				ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
		}

		public void Dispose()
		{
			vertexBuffer.Dispose();
		}

		// Draw a fullscreen quad to the given context.
		public void Draw(DeviceContext context)
		{
			context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexStride, 0));
			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			context.Draw(4, 0);
		}

		// Array of input element structures that describe the layout of vertices to D3D.
		public static InputElement[] InputElements
		{
			get
			{
				return new[] {
					new InputElement("POSITION", 0, SlimDX.DXGI.Format.R32G32B32A32_Float, 0),
				};
			}
		}

		private int VertexStride { get { return Marshal.SizeOf(typeof(Vector4)); } }

		private Buffer vertexBuffer;
	}
}
