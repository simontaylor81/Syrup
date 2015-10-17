using Newtonsoft.Json.Linq;
using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SRPTests
{
	public class DynamicJsonObjectTests
	{
		// Test primitive types.
		[Fact]
		public void TestBasicTypes()
		{
			TestBasicType(12);
			TestBasicType(3.142f);
			TestBasicType("string test");
			TestBasicType(true);

			Assert.Null(CreateDynamicObject("null"));
			Assert.Null(CreateDynamicObject("undefined"));
		}

		// Test JSON arrays.
		[Fact]
		public void TestArray()
		{
			dynamic val = CreateDynamicObject("[1, 2, 3]");
			Assert.IsType<dynamic[]>(val);
			Assert.Equal(new dynamic[] { 1, 2, 3 }, val);

			// Test array of nulls -- must not give null arg expection!
			dynamic nullarray = CreateDynamicObject("[null, null]");
			Assert.Equal(new dynamic[] { null, null }, nullarray);
		}

		// Test JSON objects.
		[Fact]
		public void TestObjects()
		{
			dynamic obj = CreateDynamicObject(@"{
				""member1"": ""value1"",
				""member2"": 7
			}");

			Assert.Equal("value1", obj.member1);
			Assert.Equal(7, obj.member2);
		}

		// Test vector and colour component access by index.
		[Fact]
		public void TestComponentIndexing()
		{
			dynamic vector = CreateDynamicObject(@"{""x"": 1, ""y"": 2, ""z"": 3, ""w"": 4}");
			Assert.Equal(1, vector[0]);
			Assert.Equal(2, vector[1]);
			Assert.Equal(3, vector[2]);
			Assert.Equal(4, vector[3]);

			dynamic colour = CreateDynamicObject(@"{""r"": 0.1, ""g"": 0.2, ""b"": 0.3, ""a"": 0.4}");
			Assert.Equal(0.1f, colour[0]);
			Assert.Equal(0.2f, colour[1]);
			Assert.Equal(0.3f, colour[2]);
			Assert.Equal(0.4f, colour[3]);
		}

		[Fact]
		public void TestNullArgument()
		{
			Assert.Throws<ArgumentNullException>(() => DynamicHelpers.CreateDynamicObject(null));
		}

		// Very simple helper to avoid verbosity.
		private dynamic CreateDynamicObject(string json)
		{
			return DynamicHelpers.CreateDynamicObject(JToken.Parse(json));
		}

		// Helper for testing basic types.
		private void TestBasicType<T>(T expected)
		{
			dynamic val = DynamicHelpers.CreateDynamicObject(new JValue(expected));
			Assert.IsType<T>(val);
			Assert.Equal(expected, val);
		}
	}
}
