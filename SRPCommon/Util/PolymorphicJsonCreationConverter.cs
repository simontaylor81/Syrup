using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SRPCommon.Util
{
	// JSON converter for creating objects based on the contents of the object.
	// Useful when you want to decide which class to generate based on a 'type' property.
	abstract class PolymorphicJsonCreationConverter<T> : JsonConverter
	{
		// Create the object instance based on its contents.
		protected abstract T Create(JObject jObject);

		public override bool CanConvert(Type objectType) => typeof(T).IsAssignableFrom(objectType);
		public override bool CanRead => true;
		public override bool CanWrite => false;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			// Read JObject from the current reader.
			var jObject = JObject.Load(reader);

			// Call overridden method to create the object.
			var result = Create(jObject);

			// Defer to the serialiser to populate the object's propeties, as per usual.
			// Create a new reader as we've already consumed the given one.
			serializer.Populate(jObject.CreateReader(), result);

			return result;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			// Should never be called, since CanWrite is false.
			throw new NotImplementedException();
		}
	}
}
