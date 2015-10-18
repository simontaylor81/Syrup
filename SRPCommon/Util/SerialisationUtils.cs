using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SlimDX;
using Newtonsoft.Json.Linq;

namespace SRPCommon.Util
{
	public class SerialisationUtils
	{
		public static Vector3 ParseVector3(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException(nameof(str));
			}

			var components = str.Split(null);
			if (components.Length == 3)
			{
				return new Vector3(
					float.Parse(components[0]),
					float.Parse(components[1]),
					float.Parse(components[2]));
			}
			throw new FormatException("Incorrect number of components for Vector3");
		}

		public static Vector4 ParseVector4(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException(nameof(str));
			}

			var components = str.Split(null);
			if (components.Length == 4)
			{
				return new Vector4(
					float.Parse(components[0]),
					float.Parse(components[1]),
					float.Parse(components[2]),
					float.Parse(components[3]));
			}
			throw new FormatException("Incorrect number of components for Vector3");
		}

		// Parse a JSON-encoded vector-3
		public static Vector3 ParseVector3(JToken obj, Vector3 defaultVal = new Vector3())
		{
			if (obj != null)
			{
				// TEMP!
				return Newtonsoft.Json.JsonConvert.DeserializeObject<Vector3>($"\"{obj.ToString()}\"");

				if (obj.Type != JTokenType.Object) throw new ArgumentException("Must be JSON object", nameof(obj));

				var x = obj["x"];
				var y = obj["y"];
				var z = obj["z"];

				if (x == null) throw new ArgumentException("Missing x component", nameof(obj));
				if (y == null) throw new ArgumentException("Missing y component", nameof(obj));
				if (z == null) throw new ArgumentException("Missing z component", nameof(obj));

				return new Vector3((float)x, (float)y, (float)z);
			}
			return defaultVal;
		}

		// Parse a JSON-encoded vector-4
		public static Vector4 ParseVector4(JToken obj, Vector4 defaultVal = new Vector4())
		{
			if (obj != null)
			{
				if (obj.Type != JTokenType.Object) throw new ArgumentException("Must be JSON object", nameof(obj));

				var x = obj["x"];
				var y = obj["y"];
				var z = obj["z"];
				var w = obj["w"];

				if (x == null) throw new ArgumentException("Missing x component", nameof(obj));
				if (y == null) throw new ArgumentException("Missing y component", nameof(obj));
				if (z == null) throw new ArgumentException("Missing z component", nameof(obj));
				if (w == null) throw new ArgumentException("Missing w component", nameof(obj));

				return new Vector4((float)x, (float)y, (float)z, (float)w);
			}
			return defaultVal;
		}

		public static void ParseAttribute(XElement element, string attribute, Action<string> parseAction)
		{
			if (element == null) throw new ArgumentNullException(nameof(element));
			if (attribute == null) throw new ArgumentNullException(nameof(attribute));
			if (parseAction == null) throw new ArgumentNullException(nameof(parseAction));

			var attr = element.Attribute(attribute);
			if (attr != null)
			{
				try
				{
					parseAction(attr.Value);
				}
				catch (FormatException)
				{
					// Ignore parse failures.
				}
			}
		}
	}
}
