﻿using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;
using Newtonsoft.Json;
using SRPCommon.UserProperties;

namespace SRPCommon.Scene
{
	// A material definition. Basically just a collection of parameters that can be bound to shader inputs.
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class Material
	{
		public string Name { get; internal set; }

		public IDictionary<string, Vector4> Parameters => vectorParameters;
		public IDictionary<string, string> Textures => textures;

		private List<IUserProperty> _userProperties = new List<IUserProperty>();
		public IEnumerable<IUserProperty> UserProperties => _userProperties;

		[JsonProperty("vectorParams")]
		private Dictionary<string, Vector4> vectorParameters = new Dictionary<string, Vector4>();
		[JsonProperty]
		private Dictionary<string, string> textures = new Dictionary<string, string>();

		internal void PostLoad()
		{
			_userProperties = vectorParameters.Keys
				.Select(key => (IUserProperty)new StructUserProperty(
					key,
					() => vectorParameters[key],
					o => vectorParameters[key] = (Vector4)o)
				)
				.ToList();

		}
	}
}
