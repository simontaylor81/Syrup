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
			_scriptTestHelper = new ScriptTestHelper("System.Numerics");
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

			// Invalid casts should throw the correct ScriptException.
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

			// Vectors themselves should be passed through unmolested.
			Assert.Equal(new Vector2(1.1f, 2.0f), ScriptHelper.ConvertToVector2(_scriptTestHelper.GetPythonValue("Vector2(1.1, 2)")));
			Assert.Equal(new Vector3(1.1f, 2.0f, 3.0f), ScriptHelper.ConvertToVector3(_scriptTestHelper.GetPythonValue("Vector3(1.1, 2, 3)")));
			Assert.Equal(new Vector4(1.1f, 2.0f, 3.0f, 4.0f), ScriptHelper.ConvertToVector4(_scriptTestHelper.GetPythonValue("Vector4(1.1, 2, 3, 4)")));
		}

		[Fact]
		public void ConvertToVector2_Invalid_Throws()
		{
			var ex = Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector2(_scriptTestHelper.GetPythonValue("1")));
			Assert.Equal("Invalid parameters for ConvertToVector2", ex.Message);
		}

		[Fact]
		public void ConvertToVector3_Invalid_Throws()
		{
			var ex = Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector3(_scriptTestHelper.GetPythonValue("lambda: 7")));
			Assert.Equal("Invalid parameters for ConvertToVector3", ex.Message);
		}

		[Theory]
		[InlineData("(1,2,3)")]
		[InlineData("Vector3(1,2,3)")]
		public void ConvertToVector4_Invalid_Throws(string val)
		{
			var ex = Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector4(_scriptTestHelper.GetPythonValue(val)));
			Assert.Equal("Invalid parameters for ConvertToVector4", ex.Message);
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
		[InlineData("Vector2(1,2)", 2)]
		[InlineData("Vector3(1,2,3)", 3)]
		[InlineData("Vector4(1,2,3,4)", 4)]
		public void CheckConvertibleFloatListValid(string expression, int numComponents)
		{
			// Valid calls complete silently without an exception.
			ScriptHelper.CheckConvertibleFloatList(_scriptTestHelper.GetPythonValue(expression), numComponents, "");
		}

		[Theory]
		[InlineData(new[] { 1.0f, 2.0f, 3.0f }, 3)]
		[InlineData(new[] { 1.0, 2.0, 3.0 }, 3)]
		[InlineData(new[] { 1, 2, 3 }, 3)]
		public void CheckConvertibleFloatList_Valid_CSArray(object array, int numComponents)
		{
			ScriptHelper.CheckConvertibleFloatList(array, numComponents, "");
		}

		[Fact]
		public void CheckConvertibleFloatList_Valid_CSVector2()
		{
			ScriptHelper.CheckConvertibleFloatList(new Vector2(1.0f, 2.0f), 2, "");
		}

		[Fact]
		public void CheckConvertibleFloatList_Valid_CSVector3()
		{
			ScriptHelper.CheckConvertibleFloatList(new Vector3(1.0f, 2.0f, 3.0f), 3, "");
		}

		[Fact]
		public void CheckConvertibleFloatList_Valid_CSVector4()
		{
			ScriptHelper.CheckConvertibleFloatList(new Vector4(1.0f, 2.0f, 3.0f, 4.0f), 4, "");
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

			// For some reason the compiler gets confused about these lambdas, hence the explicit cast to Action.
			var ex = Assert.Throws<ScriptException>((Action)(() => ScriptHelper.CheckConvertibleFloatList(_scriptTestHelper.GetPythonValue(expression), numComponents, desc)));

			// Exception should contain the description in its message.
			Assert.Contains(desc, ex.Message);
		}

		[Fact]
		public void CheckConvertibleFloatList_Invalid_CSVector_TooBig()
		{
			Assert.Throws<ScriptException>(() => ScriptHelper.CheckConvertibleFloatList(new Vector4(1.0f, 2.0f, 3.0f, 4.0f), 2, ""));
		}

		[Fact]
		public void CheckConvertibleFloatList_Invalid_CSVector_TooSmall()
		{
			Assert.Throws<ScriptException>(() => ScriptHelper.CheckConvertibleFloatList(new Vector2(1.0f, 2.0f), 4, ""));
		}
	}
}
