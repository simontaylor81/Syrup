using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ShaderUnit.TestRenderer;

namespace ShaderUnit.ShaderTests
{
	public class MyComputeShaderTest : RenderTestBase
	{
		[Test]
		public void Test()
		{
			var ri = RenderHarness.RenderInterface;

			var cs = ri.CompileShader("ComputeTest.hlsl", "Main", "cs_5_0");

			var buffer = ri.CreateBuffer(16 * 4, null, true);
			ri.SetShaderUavVariable(cs, "OutUAV", buffer);

			ri.SetFrameCallback(context =>
			{
				context.Dispatch(cs, 1, 1, 1);
			});

			RenderHarness.RenderImage();

			var result = buffer.GetContents<float>();
			Assert.That(result, Is.EqualTo(Enumerable.Range(0, 16).Select(i => 2.0f * i + 10.0f)));
		}
	}
}
