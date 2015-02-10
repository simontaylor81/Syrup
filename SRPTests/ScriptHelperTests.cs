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
			scripting = new Scripting();
		}

	    [Fact]
		public void TestResolveFunction()
		{
			// Basic values should be returned directly.
			Assert.Equal(12, ScriptHelper.Instance.ResolveFunction(GetPythonValue("12")));
			Assert.Equal("str", ScriptHelper.Instance.ResolveFunction(GetPythonValue("'str'")));
			Assert.Equal(null, ScriptHelper.Instance.ResolveFunction(null));

			// Functions return their result.
			Assert.Equal(12, ScriptHelper.Instance.ResolveFunction(GetPythonValue("lambda: 12")));

			// Functions must have zero arguments.
			Assert.Throws<ScriptException>(() => ScriptHelper.Instance.ResolveFunction(GetPythonValue("lambda x: x")));
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

		[Fact]
		public void TestCheckConvertible()
		{
			var desc = "testdescription";

			// Valid calls complete silently without an exception.
			ScriptHelper.Instance.CheckConvertibleFloat(GetPythonValue("3.142"), desc);
			ScriptHelper.Instance.CheckConvertibleFloat(GetPythonValue("3"), desc);
			ScriptHelper.Instance.CheckConvertibleFloat(GetPythonValue("lambda: 3.142"), desc);

			ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("3.142"), 1, desc);
			ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("[1, 2, 3]"), 3, desc);
			ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("(1, 2, 3)"), 3, desc);
			ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("lambda: (1,2,3)"), 3, desc);

			// Invalid calls should throw ScriptException
			// For some reason the compiler gets confused about these lambdas, hence the explicit cast to Action.
			var floatEx = Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloat(GetPythonValue("'str'"), desc)));
			Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloat(GetPythonValue("(1, 2)"), desc)));
			Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloat(null, desc)));

			var listEx = Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("'str'"), 3, desc)));
			Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("(1,2)"), 3, desc)));
			Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("3.142"), 3, desc)));
			Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloatList(GetPythonValue("('', '', '')"), 3, desc)));
			Assert.Throws<ScriptException>((Action)(() => ScriptHelper.Instance.CheckConvertibleFloatList(null, 3, desc)));

			// Exceptions should contain the description in their message.
			Assert.Contains(desc, floatEx.Message);
			Assert.Contains(desc, listEx.Message);
		}

		// Helper for getting the value of some inline python code.
		private dynamic GetPythonValue(string expression)
		{
			var source = scripting.PythonEngine.CreateScriptSourceFromString(expression, SourceCodeKind.Expression);
			return source.Execute();
        }
	}
}
