﻿using Newtonsoft.Json.Linq;
using SlimDX;
using SRPCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace SRPTests
{
	// Tests for the SRPCommon.Util.SerialisationUtils class.
	public class SerialisationUtilsTests
	{
		[Fact]
		public void TestVectorStringParse()
		{
			// Basic parsing.
			Assert.Equal(new Vector3(1.1f, 2.2f, 3.0f), SerialisationUtils.ParseVector3("1.1 2.2 3"));
			Assert.Equal(new Vector4(1.1f, 2.2f, 3.0f, 4.0f), SerialisationUtils.ParseVector4("1.1 2.2 3 4"));

			// Null argument.
			Assert.Throws<ArgumentNullException>(() => SerialisationUtils.ParseVector3(null));
			Assert.Throws<ArgumentNullException>(() => SerialisationUtils.ParseVector3(null));

			// Wrong number of components.
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector3("1 2"));
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector3("1 2 3 4"));
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector4("1 2 3"));
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector4("1 2 3 4 5"));

			// Wrong format.
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector3("1,2,3"));
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector4("1,2,3,4"));

			// Nonsense input.
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector3("Not a vector"));
			Assert.Throws<FormatException>(() => SerialisationUtils.ParseVector3("Not a vector 4"));
		}

		[Fact]
		public void TestVectorJsonParse()
		{
			// Basic parsing.
			Assert.Equal(new Vector3(1.1f, 2.2f, 3.0f), SerialisationUtils.ParseVector3(JToken.Parse("{\"x\": 1.1, \"y\": 2.2, \"z\": 3}")));
			Assert.Equal(new Vector4(1.1f, 2.2f, 3.0f, 4.0f), SerialisationUtils.ParseVector4(JToken.Parse("{\"x\": 1.1, \"y\": 2.2, \"z\": 3, \"w\": 4}")));

			// Null argument should return default value.
			var defaultVec3 = new Vector3(-1.0f, -2.0f, -3.0f);
			Assert.Equal(defaultVec3, SerialisationUtils.ParseVector3(null, defaultVec3));
			var defaultVec4 = new Vector4(-1.0f, -2.0f, -3.0f, -4.0f);
			Assert.Equal(defaultVec4, SerialisationUtils.ParseVector4(null, defaultVec4));

			// Wrong type of JToken.
			Assert.Throws<ArgumentException>(() => SerialisationUtils.ParseVector3(JToken.Parse("0")));
			Assert.Throws<ArgumentException>(() => SerialisationUtils.ParseVector4(JToken.Parse("0")));

			// Missing members.
			Assert.Throws<ArgumentException>(() => SerialisationUtils.ParseVector3(JToken.Parse("{}")));
			Assert.Throws<ArgumentException>(() => SerialisationUtils.ParseVector4(JToken.Parse("{}")));

			// Members of wrong type.
			Assert.Throws<ArgumentException>(() => SerialisationUtils.ParseVector3(JToken.Parse("{\"x\": {}, \"y\": {}, \"z\": {}}")));
			Assert.Throws<ArgumentException>(() => SerialisationUtils.ParseVector4(JToken.Parse("{\"x\": {}, \"y\": {}, \"z\": {}, \"w\": {}}")));
		}

		[Fact]
		public void TestParseAttribute()
		{
			var element = XElement.Parse("<element attr=\"value\" />");
			string val = null;

			// Successful parsing.
			SerialisationUtils.ParseAttribute(element, "attr", parsed => val = parsed);
			Assert.Equal("value", val);

			// Format exceptions in callback should be swallowed.
			SerialisationUtils.ParseAttribute(element, "attr", parsed => { throw new FormatException(); });

			// Missing attributes should be silently skipped without calling the callback.
			val = "don't change";
			SerialisationUtils.ParseAttribute(element, "missing", parsed => val = parsed);
			Assert.Equal("don't change", val);

			// Null arguments should throw.
			Assert.Throws<ArgumentNullException>(() => SerialisationUtils.ParseAttribute(null, "attr", x => { }));
			Assert.Throws<ArgumentNullException>(() => SerialisationUtils.ParseAttribute(element, null, x => { }));
			Assert.Throws<ArgumentNullException>(() => SerialisationUtils.ParseAttribute(element, "attr", null));
		}
	}
}