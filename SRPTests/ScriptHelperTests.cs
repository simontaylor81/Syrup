using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Scripting;
using SlimDX;
using SRPCommon.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SRPTests
{
	// Tests for SRPCommon.Scripting.ScriptHelper
	public class ScriptHelperTests
	{
		private Scripting scripting;

		// Common initialisation
		public ScriptHelperTests()
		{
			// Create Scripting object, which initialises the script engine.
			scripting = new Scripting(null);
		}

		[Theory]
		[InlineData(12, "12")]
		[InlineData("str", "'str'")]
		[InlineData(null, "None")]
		[InlineData(12, "lambda: 12")]
		public void ResolveFunctionValid(object expected, string expression)
		{
			Assert.Equal(expected, ScriptHelper.Instance.ResolveFunction(GetPythonValue(expression)));
		}

		[Theory]
		[InlineData("lambda x: x")]		// Functions must have zero arguments.
		public void ResolveFunctionInvalid(string expression)
		{
			Assert.Throws<ScriptException>(() => ScriptHelper.Instance.ResolveFunction(GetPythonValue(expression)));
		}

		[Fact]
		public void TestGuardedCast()
		{
			// Test valid casts.
			Assert.Equal(3.142f, ScriptHelper.GuardedCast<float>(GetPythonValue("3.142")));
			Assert.Equal("str", ScriptHelper.GuardedCast<string>(GetPythonValue("'str'")));

			// Invalid casts should through the correct ScriptException.
			Assert.Throws<ScriptException>(() => ScriptHelper.GuardedCast<float>(null));
			Assert.Throws<ScriptException>(() => ScriptHelper.GuardedCast<float>(GetPythonValue("'str'")));
		}

		[Fact]
		public void TestConversions()
		{
			// Test regular functioning of each function.
			Assert.Equal(new Vector2(1.1f, 2.0f), ScriptHelper.ConvertToVector2(GetPythonValue("(1.1, 2)")));
			Assert.Equal(new Vector3(1.1f, 2.0f, 3.0f), ScriptHelper.ConvertToVector3(GetPythonValue("(1.1, 2, 3)")));
			Assert.Equal(new Vector4(1.1f, 2.0f, 3.0f, 4.0f), ScriptHelper.ConvertToVector4(GetPythonValue("(1.1, 2, 3, 4)")));
			Assert.Equal(new Color3(1.1f, 2.0f, 3.0f), ScriptHelper.ConvertToColor3(GetPythonValue("(1.1, 2, 3)")));
			Assert.Equal(new Color4(4.0f, 1.1f, 2.0f, 3.0f), ScriptHelper.ConvertToColor4(GetPythonValue("(1.1, 2, 3, 4)")));

			// Python lists should work too.
			Assert.Equal(new Vector4(1.1f, 2.0f, 3.0f, 4.0f), ScriptHelper.ConvertToVector4(GetPythonValue("[1.1, 2, 3, 4]")));

			// Test various invalid values
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector2(GetPythonValue("1")));
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector3(GetPythonValue("lambda: 7")));
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToVector4(GetPythonValue("(1,2,3)")));
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToColor3(GetPythonValue("('', '', '')")));
			Assert.Throws<ScriptException>(() => ScriptHelper.ConvertToColor4(null));
		}

		[Theory]
		[InlineData("3.142")]
		[InlineData("3")]
		[InlineData("lambda: 3.142")]
		public void CheckConvertibleFloatValid(string expression)
		{
			// Valid calls complete silently without an exception.
			ScriptHelper.Instance.CheckConvertibleFloat(GetPythonValue(expression), "");
		}

		[Theory]
		[InlineData("3.142", 1)]
		[InlineData("[1, 2, 3]", 3)]
		[InlineData("(1, 2, 3)", 3)]
		[InlineData("lambda: (1,2,3)", 3)]
		public void CheckConvertibleFloatListValid(string expression, int numComponents)
		{
			// Valid calls complete silently without an exception.
			ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue(expression), numComponents, "");
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
			var ex = Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloat(GetPythonValue(expression), desc)));

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
			var ex = Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue(expression), numComponents, desc)));

			// Exception should contain the description in its message.
			Assert.Contains(desc, ex.Message);
		}

		// Helper for getting the value of some inline python code.
		private dynamic GetPythonValue(string expression)
		{
			var source = scripting.PythonEngine.CreateScriptSourceFromString(expression, SourceCodeKind.Expression);
			return source.Execute();
		}
	}
}
