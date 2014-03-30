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
				return new Vector3(
					(float)obj["x"],
					(float)obj["y"],
					(float)obj["z"]
				);
			}
			return defaultVal;
		}

		// Parse a JSON-encoded vector-4
		public static Vector4 ParseVector4(JToken obj, Vector4 defaultVal = new Vector4())
		{
			if (obj != null)
			{
				return new Vector4(
					(float)obj["x"],
					(float)obj["y"],
					(float)obj["z"],
					(float)obj["w"]
				);
			}
			return defaultVal;
		}

		public static void ParseAttribute(XElement element, string attribute, Action<string> parseAction)
		{
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
