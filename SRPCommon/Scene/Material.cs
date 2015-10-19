using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;
using Newtonsoft.Json;

namespace SRPCommon.Scene
{
	// A material definition. Basically just a collection of parameters that can be bound to shader inputs.
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class Material
	{
		public string Name { get; internal set; }

		public IDictionary<string, Vector4> Parameters => vectorParameters;
		public IDictionary<string, string> Textures => textures;

		[JsonProperty("vectorParams")]
		private Dictionary<string, Vector4> vectorParameters = new Dictionary<string, Vector4>();
		[JsonProperty]
		private Dictionary<string, string> textures = new Dictionary<string, string>();
	}
}
