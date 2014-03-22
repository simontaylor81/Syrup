using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SlimDX;

namespace ShaderEditorApp.Scene
{
	// A material definition. Basically just a collection of parameters that can be bound to shader inputs.
	public class Material
	{
		public string Name { get; private set; }

		public IDictionary<string, Vector4> Parameters { get { return vectorParameters; } }
		public IDictionary<string, string> Textures { get { return textures; } }

		private Dictionary<string, Vector4> vectorParameters;
		private Dictionary<string, string> textures;

		public static Material Load(XElement element)
		{
			Debug.Assert(element.Name == "material");

			var result = new Material();

			SerialisationUtils.ParseAttribute(element, "name", str => result.Name = str);

			// Load vector params.
			var vecParams = from paramElem in element.Descendants("vectorParam") select LoadVectorParam(paramElem);
			result.vectorParameters = (from param in vecParams where param != null select param).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

			// Load texture references.
			var textures = from texElem in element.Descendants("texture") select LoadTexture(texElem);
			result.textures = (from texture in textures where texture != null select texture).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

			return result;
		}

		private static Tuple<string, Vector4> LoadVectorParam(XElement element)
		{
			var nameAttr = element.Attribute("name");
			var valueAttr = element.Attribute("value");

			if (nameAttr != null && valueAttr != null)
			{
				try
				{
					var value = SerialisationUtils.ParseVector4(valueAttr.Value);
					return Tuple.Create(nameAttr.Value, value);
				}
				catch (FormatException)
				{
					// Do nothing, just fall through and return null.
					// TODO: Log error.
				}
			}

			return null;
		}

		private static Tuple<string, string> LoadTexture(XElement element)
		{
			var nameAttr = element.Attribute("name");
			var filenameAttr = element.Attribute("filename");

			if (nameAttr != null && filenameAttr != null)
			{
				return Tuple.Create(nameAttr.Value, filenameAttr.Value);
			}

			return null;
		}
	}
}
