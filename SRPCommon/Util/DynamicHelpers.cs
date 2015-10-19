using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SRPCommon.Util
{
	// Helpers for handling dynamic objects.
	public static class DynamicHelpers
	{
		/// <summary>
		/// Given a JSON.NET JToken object, create a dynamic object that represents it.
		/// JTokens can be used as dynamic already, but everything is wrapped
		/// in JValue objects, which makes it a pain to use in the scripts.
		/// </summary>
		public static dynamic CreateDynamicObject(JToken token)
		{
			if (token == null)
			{
				throw new ArgumentNullException(nameof(token));
			}

			switch (token.Type)
			{
				case JTokenType.Object:
					return new DynamicJsonObject((JObject)token);

				case JTokenType.Array:
					return ((IEnumerable<dynamic>)token)		// Cast to enumerable so we can use extension methods
						.Select(x => CreateDynamicObject(x))	// Recursively convert each entry
						.ToArray();								// Convert to immutable array

				case JTokenType.Integer:
					return (int)token;

				case JTokenType.Float:
					return (float)token;

				case JTokenType.String:
					return (string)token;

				case JTokenType.Boolean:
					return (bool)token;
			}

			return null;
		}

		// Implementation of DynamicObject for our JSON -> dynamic converter.
		private class DynamicJsonObject : DynamicObject
		{
			// Dictionary used for storing the members.
			private Dictionary<string, dynamic> dictionary = new Dictionary<string, dynamic>();

			// Array for accessing vector/colour components by index.
			private dynamic[] components = null;

			public DynamicJsonObject(JObject obj)
			{
				// As a bit of a fudge, we allow vector and colour component properties
				// to be accessed by index.
				var vectorComponentNames = new[] { "x", "y", "z", "w" };
				var colourComponentNames = new[] { "r", "g", "b", "a" };
				var newComponents = new List<dynamic>();

				// Add each property to the dictionary.
				foreach (var prop in obj)
				{
					// Recurse to convert value.
					var subobject = CreateDynamicObject(prop.Value);
					dictionary.Add(prop.Key, subobject);

					// Is this a vector or colour component?
					var nameLower = prop.Key.ToLowerInvariant();
					var componentIndex = Array.IndexOf(vectorComponentNames, nameLower);
					if (componentIndex == -1)
					{
						componentIndex = Array.IndexOf(colourComponentNames, nameLower);
					}
					if (componentIndex >= 0)
					{
						if (newComponents.Count < componentIndex)
						{
							// Pad to desired size with nulls.
							newComponents.AddRange(Enumerable.Repeat<dynamic>(null, componentIndex - 1));
						}
						newComponents.Add(subobject);
					}
				}

				if (newComponents.Count > 0)
				{
					components = newComponents.ToArray();
				}
			}

			// Method that is called when trying to access a property of the object.
			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				return dictionary.TryGetValue(binder.Name, out result);
			}

			// Return all dynamic member names.
			public override IEnumerable<string> GetDynamicMemberNames() => dictionary.Keys;

			// Allow accessing components array by index, if we have one.
			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
			{
				int index = (int)indexes[0];
				if (components != null && index >= 0 && index < components.Length)
				{
					result = components[index];
					return true;
				}
				return base.TryGetIndex(binder, indexes, out result);
			}
		}
	}

	public class JsonDynamicObjectConverter : JsonConverter
	{
		// Standalone serialiser that does not do stuff like camel-casing.
		private readonly JsonSerializer _rawSerializer = new JsonSerializer();

		public override bool CanConvert(Type objectType) => objectType == typeof(object);
		public override bool CanRead => true;
		public override bool CanWrite => true;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			// Read JToken from the current reader.
			var token = JToken.Load(reader);

			// Create dynamic object from it.
			return DynamicHelpers.CreateDynamicObject(token);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			// Use raw serialiser to avoid camel-casing.
			_rawSerializer.Serialize(writer, value);
		}
	}
}
