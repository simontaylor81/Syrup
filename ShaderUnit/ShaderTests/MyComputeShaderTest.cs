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
			var cs = RenderHarness.RenderInterface.CompileShader("ComputeTest.hlsl", "WriteToUAV", "cs_5_0");

			var result = RenderHarness.DispatchToBuffer<float>(cs, "OutUAV", Tuple.Create(16, 1, 1), Tuple.Create(16, 1, 1));
			Assert.That(result, Is.EqualTo(Enumerable.Range(0, 16).Select(i => 2.0f * i + 10.0f)));
		}

		[Test]
		public void ReadFromBuffer()
		{
			var ri = RenderHarness.RenderInterface;

			var cs = ri.CompileShader("ComputeTest.hlsl", "ReadFromBuffer", "cs_5_0");

			// Input buffer.
			var input = Enumerable.Range(0, 16).Select(x => (float)x);
			var inputBuffer = ri.CreateStructuredBuffer(input);
			ri.SetShaderResourceVariable(cs, "InBuffer", inputBuffer);

			var result = RenderHarness.DispatchToBuffer<float>(cs, "OutUAV", Tuple.Create(16, 1, 1), Tuple.Create(16, 1, 1));
			Assert.That(result, Is.EqualTo(input.Select(x => 2.0f * x)));
		}
	}
}
