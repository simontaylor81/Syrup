using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ShaderUnit.TestRenderer;
using SlimDX;

namespace SRPTests.ShaderTests
{
	public class MyShaderUnitTest : RenderTestBase
	{
		[Test]
		public void NoParams_ReturnFloat()
		{
			var result = RenderHarness.ExecuteShaderFunction<float>("UnitTests.hlsl", "NoParams_ReturnFloat");
			Assert.That(result, Is.EqualTo(12.0f));
		}

		[Test]
		public void NoParams_ReturnInt()
		{
			var result = RenderHarness.ExecuteShaderFunction<int>("UnitTests.hlsl", "NoParams_ReturnInt");
			Assert.That(result, Is.EqualTo(57));
		}

		[Test]
		public void NoParams_ReturnFloat2()
		{
			var result = RenderHarness.ExecuteShaderFunction<Vector2>("UnitTests.hlsl", "NoParams_ReturnFloat2");
			Assert.That(result, Is.EqualTo(new Vector2(11.0f, 12.0f)));
		}

		[Test]
		public void NoParams_ReturnFloat3()
		{
			var result = RenderHarness.ExecuteShaderFunction<Vector3>("UnitTests.hlsl", "NoParams_ReturnFloat3");
			Assert.That(result, Is.EqualTo(new Vector3(11.0f, 12.0f, 13.0f)));
		}

		[Test]
		public void NoParams_ReturnFloat4()
		{
			var result = RenderHarness.ExecuteShaderFunction<Vector4>("UnitTests.hlsl", "NoParams_ReturnFloat4");
			Assert.That(result, Is.EqualTo(new Vector4(11.0f, 12.0f, 13.0f, 14.0f)));
		}
	}
}
