using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SRPCommon.Util
{
	public static class SerialisationUtils
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
