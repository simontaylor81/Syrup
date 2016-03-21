using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace SRPRendering.Shaders
{
	class ConstantBuffer : IDisposable
	{
		private ShaderConstantVariable[] variables;

		private readonly DataBox _contents;
		private readonly DataStream _stream;

		public string Name { get; }
		public SharpDX.Direct3D11.Buffer Buffer { get; }
		public IEnumerable<ShaderConstantVariable> Variables => variables;

		public ConstantBuffer(Device device, SharpDX.D3DCompiler.ConstantBuffer bufferInfo)
		{
			Name = bufferInfo.Description.Name;

			// Gather info about the variables in this buffer.
			variables = bufferInfo.GetVariables()
				.Select(variable => new ShaderConstantVariable(variable))
				.ToArray();

			// Create a data stream containing the initial contents buffer.
			_stream = new DataStream(bufferInfo.Description.Size, true, true);
			_contents = new DataBox(_stream.DataPointer);

			// Write initial values to buffer.
			foreach (var variable in variables)
			{
				variable.WriteToBuffer(_stream);
			}

			// Create the actual buffer.
			_stream.Position = 0;
			Buffer = new SharpDX.Direct3D11.Buffer(
				device,
				_stream,
				bufferInfo.Description.Size,
				ResourceUsage.Default,
				BindFlags.ConstantBuffer,
				CpuAccessFlags.None,
				ResourceOptionFlags.None,
				0);

		}

		public void Dispose()
		{
			Buffer.Dispose();
		}

		// Upload the constants to the buffer if dirty.
		public void Update(DeviceContext context)
		{
			bool bDirty = false;
			foreach (var variable in variables)
			{
				bDirty |= variable.WriteToBuffer(_stream);
			}

			if (bDirty)
			{
				_stream.Position = 0;
				context.UpdateSubresource(_contents, Buffer, 0);
			}
		}
	}
}
