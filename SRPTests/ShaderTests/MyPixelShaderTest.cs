using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SlimDX;
using SRPTests.TestRenderer;

namespace SRPTests.ShaderTests
{
	public class MyPixelShaderTest : IDisposable
	{
		private readonly RenderTestContext _context;
		private readonly RenderTestHarness _renderHarness;

		public MyPixelShaderTest()
		{
			_renderHarness = new RenderTestHarness();
			//_context = context;
		}

		public void Dispose()
		{
			_renderHarness.Dispose();
		}

		[Test]
		public async Task MyTest()
		{
			var ri = _renderHarness.RenderInterface;

			var vs = ri.CompileShader("ConstantColour.hlsl", "VS", "vs_4_0");
			var ps = ri.CompileShader("ConstantColour.hlsl", "PS", "ps_4_0");

			ri.SetShaderVariable(ps, "Colour", new[] { 1, 0, 0, 1 });

			ri.SetFrameCallback(context =>
			{
				context.DrawFullscreenQuad(vs, ps);
			});

			await _renderHarness.Go($"MyPixelShaderTest");
		}

		[Test]
		[TestCase(0, 1, 0, 1)]
		[TestCase(0, 0, 1, 1)]
		public async Task MyParameterisedTest(float r, float g, float b, float a)
		{
			var ri = _renderHarness.RenderInterface;

			var vs = ri.CompileShader("ConstantColour.hlsl", "VS", "vs_4_0");
			var ps = ri.CompileShader("ConstantColour.hlsl", "PS", "ps_4_0");

			ri.SetShaderVariable(ps, "Colour", new[] { r, g, b, a });

			ri.SetFrameCallback(context =>
			{
				context.DrawFullscreenQuad(vs, ps);
			});

			await _renderHarness.Go($"MyPixelShaderTest2");
		}
	}
}
