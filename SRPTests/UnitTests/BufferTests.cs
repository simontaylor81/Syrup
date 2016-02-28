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
			using (var buffer = new Buffer(_device.Device, 4 * sizeof(float), sizeof(float), false, null))
			{
				var contents = buffer.GetContents<float>();
				Assert.Equal(contents, new[] { 0.0f, 0.0f, 0.0f, 0.0f });
			}
		}

		[Theory]
		[InlineData("[0, 1, 2, 3, 4, 5, 6, 7]", 8)]
		[InlineData("[0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0]", 8)]
		[InlineData("lambda x: x", 64)]
		public void Buffer_from_python_scalar(string expression, int numElements)
		{
			// Arrange.
			var contents = _scriptTestHelper.GetPythonValue(expression);
			var format = Format.R32_Float;

			// Act.
			using (Buffer buffer = Buffer.CreateDynamic(_device.Device, numElements * format.Size(), false, format, contents))
			{
				// Assert.
				var result = buffer.GetContents<float>();
				Assert.Equal(result, Enumerable.Range(0, numElements).Select(x => (float)x));
			}
		}

		[Theory]
		[InlineData("[(0, 1, 2, 3), (10, 11, 12, 13), (20, 21, 22, 23), (30, 31, 32, 33)]", 4)]
		[InlineData("lambda x: (10 * x + 0, 10 * x + 1, 10 * x + 2, 10 * x + 3)", 64)]
		public void Buffer_from_python_vector(string expression, int numElements)
		{
			// Arrange.
			var contents = _scriptTestHelper.GetPythonValue(expression);
			var format = Format.R32G32B32A32_Float;

			// Act.
			using (Buffer buffer = Buffer.CreateDynamic(_device.Device, numElements * format.Size(), false, format, contents))
			{
				// Assert.
				var result = buffer.GetContents<Vector4>();
				Assert.Equal(Enumerable.Range(0, numElements).Select(x => new Vector4(10 * x + 0, 10 * x + 1, 10 * x + 2, 10 * x + 3)), result);
			}
		}
	}
}
