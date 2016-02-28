using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SRPTests.Util;
using Xunit;

namespace SRPTests.UnitTests
{
	// Tests for using System.Numerics vectors from script.
	public class ScriptVectorTests
	{
		private readonly ScriptTestHelper _scriptTestHelper;

		public ScriptVectorTests()
		{
			_scriptTestHelper = new ScriptTestHelper("System.Numerics");
		}

		[Fact]
		public void CreateVector3()
		{
			// Test we can use vectors from python.
			Vector3 vec = _scriptTestHelper.GetPythonValue("Vector3(1, 2, 3)");
			Assert.Equal(new Vector3(1.0f, 2.0f, 3.0f), vec);
		}

		[Fact]
		public void VectorOperator()
		{
			// Test that we can use vector operators.
			Vector3 result = _scriptTestHelper.GetPythonValue("Vector3(1, 2, 3) + Vector3(4, 5, 6)");
			Assert.Equal(new Vector3(5.0f, 7.0f, 9.0f), result);
		}

		[Fact]
		public void VectorMemberFunction()
		{
			// Test that we can use vector member functions.
			double result = _scriptTestHelper.GetPythonValue("Vector3(1, 2, 3).LengthSquared()");
			Assert.Equal(14.0, result);
		}

		[Fact]
		public void VectorStaticFunction()
		{
			// Test that we can use static vector functions.
			double result = _scriptTestHelper.GetPythonValue("Vector3.Dot(Vector3(1, 2, 3), Vector3(4, 5, 6))");
			Assert.Equal(32.0, result);
		}
	}
}
