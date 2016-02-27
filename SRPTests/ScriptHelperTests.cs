using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Scripting;
using SRPTests.Util;
using Xunit;

namespace SRPTests
{
	// Tests for SRPCommon.Scripting.ScriptHelper
	public class ScriptHelperTests
	{
		private readonly ScriptTestHelper _scriptTestHelper;

		// Common initialisation
		public ScriptHelperTests()
		{
			// Create Scripting object, which initialises the script engine.
			_scriptTestHelper = new ScriptTestHelper();
		}

		[Theory]
		[InlineData(12, "12")]
		[InlineData("str", "'str'")]
		[InlineData(null, "None")]
		[InlineData(12, "lambda: 12")]
		public void ResolveFunctionValid(object expected, string expression)
		{
			Assert.Equal(expected, ScriptHelper.ResolveFunction(_scriptTestHelper.GetPythonValue(expression)));
		}

		[Theory]
		[InlineData("lambda x: x")]		// Functions must have zero arguments.
		public void ResolveFunctionInvalid(string expression)
		{
			Assert.Throws<ScriptException>(() => ScriptHelper.ResolveFunction(_scriptTestHelper.GetPythonValue(expression)));
		}

		[Fact]
		public void TestGuardedCast()
		{
			// Test valid casts.
			Assert.Equal(3.142f, ScriptHelper.GuardedCast<float>(_scriptTestHelper.GetPythonValue("3.142")));
			Assert.Equal("str", ScriptHelper.GuardedCast<string>(_scriptTestHelper.GetPythonValue("'str'")));

			// Invalid casts should through the correct ScriptException.
			Assert.Throws<ScriptException>(() => ScriptHelper.GuardedCast<float>(null));
			Assert.Throws<ScriptException>(() => ScriptHelper.GuardedCast<float>(_scriptTestHelper.GetPythonValue("'str'")));
		}

		[Fact]
		public void TestConversions()
		{
			// Test regular functioning of each function.
			Assert.Equal(new Vector2(1.1f, 2.0f), ScriptHelper.ConvertToVector2(_scriptTestHelper.GetPythonValue("(1.1, 2)")));
			Assert.Equal(new Vector3(1.1f, 2.0f, 3.0f), ScriptHelper.ConvertToVector3(_scriptTestHelper.GetPythonValue("(1.1, 2, 3)")));
			Assert.Equal(new Vector4(1.1f, 2.0f, 3.0f, 4.0f), ScriptHelper.ConvertToVector4(_scriptTestHelper.GetPythonValue("(1.1, 2, 3, 4)")));

			// Python lists should work too.
			Assert.Equal(new Vector4(1.1f, 2.0f, 3.0f, 4.0f), ScriptHelper.ConvertToVector4(_scriptTestHelper.GetPythonValue("[1.1, 2, 3, 4]")));

			// Test various invalid values
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector2(_scriptTestHelper.GetPythonValue("1")));
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector3(_scriptTestHelper.GetPythonValue("lambda: 7")));
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector4(_scriptTestHelper.GetPythonValue("(1,2,3)")));
		}

		[Theory]
		[InlineData("3.142")]
		[InlineData("3")]
		[InlineData("lambda: 3.142")]
		public void CheckConvertibleFloatValid(string expression)
		{
			// Valid calls complete silently without an exception.
			ScriptHelper.CheckConvertibleFloat(_scriptTestHelper.GetPythonValue(expression), "");
		}

		[Theory]
		[InlineData("3.142", 1)]
		[InlineData("[1, 2, 3]", 3)]
		[InlineData("(1, 2, 3)", 3)]
		[InlineData("lambda: (1,2,3)", 3)]
		public void CheckConvertibleFloatListValid(string expression, int numComponents)
		{
			// Valid calls complete silently without an exception.
			ScriptHelper.CheckConvertibleFloatList(_scriptTestHelper.GetPythonValue(expression), numComponents, "");
		}

		[Fact]
		public void CheckConvertibleFloatList_CS_FloatArray()
		{
			ScriptHelper.CheckConvertibleFloatList(new[] { 1.0f, 2.0f, 3.0f }, 3, "");
		}

		[Fact]
		public void CheckConvertibleFloatList_CS_DoubleArray()
		{
			ScriptHelper.CheckConvertibleFloatList(new[] { 1.0, 2.0, 3.0 }, 3, "");
		}

		[Fact]
		public void CheckConvertibleFloatList_CS_IntArray()
		{
			ScriptHelper.CheckConvertibleFloatList(new[] { 1, 2, 3}, 3, "");
		}

		[Theory]
		[InlineData("'str'")]
		[InlineData("(1,2)")]
		[InlineData("None")]
		public void CheckConvertibleFloatInvalid(string expression)
		{
			var desc = "testdescription";

			// Invalid calls should throw ScriptException
			// For some reason the compiler gets confused about these lambdas, hence the explicit cast to Action.
			var ex = Assert.Throws<ScriptException>((Action)(() => ScriptHelper.CheckConvertibleFloat(_scriptTestHelper.GetPythonValue(expression), desc)));

			// Exception should contain the description in its message.
			Assert.Contains(desc, ex.Message);
		}

		[Theory]
		[InlineData("'str'", 3)]
		[InlineData("(1,2)", 3)]
		[InlineData("3.142", 3)]
		[InlineData("('', '', '')", 3)]
		[InlineData("None", 3)]
		public void CheckConvertibleFloatListInvalid(string expression, int numComponents)
		{
			var desc = "testdescription";

			// Invalid calls should throw ScriptException
			// For some reason the compiler gets confused about these lambdas, hence the explicit cast to Action.
			var ex = Assert.Throws<ScriptException>((Action)(() => ScriptHelper.CheckConvertibleFloatList(_scriptTestHelper.GetPythonValue(expression), numComponents, desc)));

			// Exception should contain the description in its message.
			Assert.Contains(desc, ex.Message);
		}
	}
}
