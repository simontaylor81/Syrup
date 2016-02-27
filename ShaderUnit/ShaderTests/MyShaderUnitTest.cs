using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ShaderUnit.TestRenderer;

namespace ShaderUnit.ShaderTests
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

		[Test]
		public void OneFloatParam()
		{
			var result = RenderHarness.ExecuteShaderFunction<float>("UnitTests.hlsl", "OneFloatParam", 12.0f);
			Assert.That(result, Is.EqualTo(13.0f));
		}

		[Test]
		public void TwoFloatParams()
		{
			var result = RenderHarness.ExecuteShaderFunction<float>("UnitTests.hlsl", "TwoFloatParams", 12.0f, 13.0f);
			Assert.That(result, Is.EqualTo(25.0f));
		}

		[Test]
		public void OneFloat2Param()
		{
			var result = RenderHarness.ExecuteShaderFunction<float>("UnitTests.hlsl", "OneFloat2Param", new Vector2(1.0f, 2.0f));
			Assert.That(result, Is.EqualTo(5.0f));
		}

		[Test]
		public void OneFloat3Param()
		{
			var result = RenderHarness.ExecuteShaderFunction<float>("UnitTests.hlsl", "OneFloat3Param", new Vector3(1.0f, 2.0f, 3.0f));
			Assert.That(result, Is.EqualTo(14.0f));
		}

		[Test]
		public void OneFloat4Param()
		{
			var result = RenderHarness.ExecuteShaderFunction<float>("UnitTests.hlsl", "OneFloat4Param", new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
			Assert.That(result, Is.EqualTo(30.0f));
		}
	}
}
