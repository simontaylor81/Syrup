using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ShaderUnit.TestRenderer;
using SRPScripting;

namespace ShaderUnit.ShaderTests
{
	public class MyComputeShaderTest : RenderTestBase
	{
		[Test]
		public void WriteToUAV()
		{
			var ri = RenderHarness.RenderInterface;

			var cs = ri.CompileShader("ComputeTest.hlsl", "WriteToUAV", "cs_5_0");

			var buffer = ri.CreateBuffer(16 * 4, Format.R32_Float, null, uav: true);
			ri.SetShaderUavVariable(cs, "OutUAV", buffer);

			ri.SetFrameCallback(context =>
			{
				context.Dispatch(cs, 1, 1, 1);
			});

			RenderHarness.RenderImage();

			var result = buffer.GetContents<float>();
			Assert.That(result, Is.EqualTo(Enumerable.Range(0, 16).Select(i => 2.0f * i + 10.0f)));
		}

		[Test]
		public void ReadFromBuffer()
		{
			var ri = RenderHarness.RenderInterface;

			var cs = ri.CompileShader("ComputeTest.hlsl", "ReadFromBuffer", "cs_5_0");

			// Output buffer.
			var outputBuffer = ri.CreateBuffer(16 * 4, Format.R32_Float, null, uav: true);
			ri.SetShaderUavVariable(cs, "OutUAV", outputBuffer);

			// Input buffer.
			var input = Enumerable.Range(0, 16).Select(x => (float)x);
			var inputBuffer = ri.CreateStructuredBuffer(input);
			ri.SetShaderResourceVariable(cs, "InBuffer", inputBuffer);

			ri.SetFrameCallback(context =>
			{
				context.Dispatch(cs, 1, 1, 1);
			});

			RenderHarness.Dispatch();

			var result = outputBuffer.GetContents<float>();
			Assert.That(result, Is.EqualTo(input.Select(x => 2.0f * x)));
		}
	}
}
