using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SRPTests.TestRenderer;
using Xunit;

namespace SRPTests.ShaderTests
{
	public class MyPixelShaderTest : IDisposable, IClassFixture<TestReporter>
	{
		private readonly RenderTestContext _context;
		private readonly RenderTestHarness _renderHarness;

		public MyPixelShaderTest(TestReporter reporter, RenderTestContext context)
		{
			_renderHarness = new RenderTestHarness(reporter);
			_context = context;
		}

		public void Dispose()
		{
			_renderHarness.Dispose();
		}

		[RenderFact]
		public async Task MyFact()
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

		[RenderTheory]
		[InlineData(0, 1, 0, 1)]
		[InlineData(0, 0, 1, 1)]
		public async Task MyTheory(float r, float g, float b, float a)
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
