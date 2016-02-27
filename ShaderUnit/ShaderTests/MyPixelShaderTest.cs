using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ShaderUnit.TestRenderer;

namespace ShaderUnit.ShaderTests
{
	public class MyPixelShaderTest : RenderTestBase
	{
		[Test]
		public void MyTest()
		{
			var ri = RenderHarness.RenderInterface;

			var vs = ri.CompileShader("ConstantColour.hlsl", "VS", "vs_4_0");
			var ps = ri.CompileShader("ConstantColour.hlsl", "PS", "ps_4_0");

			ri.SetShaderVariable(ps, "Colour", new[] { 1, 0, 0, 1 });

			var result = RenderHarness.RenderFullscreenImage(vs, ps);
			CompareImage(result);
		}

		[TestCase(0, 1, 0, 1)]
		[TestCase(0, 0, 1, 1)]
		public void MyParameterisedTest(float r, float g, float b, float a)
		{
			var ri = RenderHarness.RenderInterface;

			var vs = ri.CompileShader("ConstantColour.hlsl", "VS", "vs_4_0");
			var ps = ri.CompileShader("ConstantColour.hlsl", "PS", "ps_4_0");

			ri.SetShaderVariable(ps, "Colour", new[] { r, g, b, a });

			ri.SetFrameCallback(context =>
			{
				context.DrawFullscreenQuad(vs, ps);
			});

			var result = RenderHarness.RenderImage();
			CompareImage(result);
		}
	}
}
