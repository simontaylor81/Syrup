using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SRPRendering;
using SRPScripting;
using SRPTests.Util;
using Xunit;

using Buffer = SRPRendering.Resources.Buffer;

namespace SRPTests.UnitTests
{
	public class BufferTests
	{
		private readonly RenderDevice _device;
		private readonly ScriptTestHelper _scriptTestHelper;

		public BufferTests()
		{
			_device = new RenderDevice(useWarp: true);
			_scriptTestHelper = new ScriptTestHelper();
		}

		[Fact]
		public void Buffer_without_initial_data_is_all_zeros()
		{
			using (var buffer = new Buffer(_device.Device, 4 * sizeof(float), sizeof(float), false, false, null))
			{
				var contents = buffer.GetContents<float>();
				Assert.Equal(contents, new[] { 0.0f, 0.0f, 0.0f, 0.0f });
			}
		}
	}
}
