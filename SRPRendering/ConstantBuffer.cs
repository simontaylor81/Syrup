using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.Direct3D11;

namespace SRPRendering
{
	class ConstantBuffer : IDisposable
	{
		private ShaderVariable[] variables;
		private DataBox contents;

		public string Name { get; }
		public SlimDX.Direct3D11.Buffer Buffer { get; }
		public IEnumerable<IShaderVariable> Variables => variables;

		public ConstantBuffer(Device device, SlimDX.D3DCompiler.ConstantBuffer bufferInfo)
		{
			Name = bufferInfo.Description.Name;

			// Gather info about the variables in this buffer.
			variables = (from i in Enumerable.Range(0, bufferInfo.Description.Variables)
						 select new ShaderVariable(bufferInfo.GetVariable(i))).ToArray();

			// Create a data stream containing the initial contents buffer.
			var stream = new DataStream(bufferInfo.Description.Size, true, true);
			contents = new DataBox(bufferInfo.Description.Size, bufferInfo.Description.Size, stream);

			// Write initial values to buffer.
			foreach (var variable in variables)
				variable.WriteToBuffer(stream);

			// Create the actual buffer.
			stream.Position = 0;
			Buffer = new SlimDX.Direct3D11.Buffer(
				device,
				stream,
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
				bDirty |= variable.WriteToBuffer(contents.Data);

			if (bDirty)
			{
				contents.Data.Position = 0;
				context.UpdateSubresource(contents, Buffer, 0);
			}
		}
	}
}
