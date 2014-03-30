using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SlimDX;
using Newtonsoft.Json.Linq;
using SRPCommon.Util;

namespace SRPCommon.Scene
{
	// A material definition. Basically just a collection of parameters that can be bound to shader inputs.
	public class Material
	{
		public string Name { get; private set; }

		public IDictionary<string, Vector4> Parameters { get { return vectorParameters; } }
		public IDictionary<string, string> Textures { get { return textures; } }

		private Dictionary<string, Vector4> vectorParameters;
		private Dictionary<string, string> textures;

		public static Material Load(JToken obj)
		{
			var result = new Material();
			result.Name = (string)obj["name"];

			// Load vector params.
			result.vectorParameters = obj["vectorParams"]
				.EmptyIfNull()		// Missing value means no vector params
				.Select(paramObj => LoadVectorParam(paramObj))
				.Where(param => param != null)
				.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

			// Load texture references.
			result.textures = obj["textures"]
				.EmptyIfNull()		// Missing value means no textures
				.Select(texObj => LoadTexture(texObj))
				.Where(texture => texture != null)
				.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

			return result;
		}

		private static Tuple<string, Vector4> LoadVectorParam(JToken obj)
		{
			var name = (string)obj["name"];
			var value = SerialisationUtils.ParseVector4(obj["value"]);
			return (name != null && value != null) ? Tuple.Create(name, value) : null;
		}

		private static Tuple<string, string> LoadTexture(JToken obj)
		{
			var name = (string)obj["name"];
			var value = (string)obj["filename"];
			return (name != null && value != null) ? Tuple.Create(name, value) : null;
		}
	}
}
