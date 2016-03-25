using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SRPCommon.Logging;
using SRPCommon.Util;

namespace SRPCommon.Scene
{
	// Implementation of load and save functions for scenes.
	// Split into a separate file simply for cleanliness.
	public partial class Scene
	{
		static private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
		{
			Formatting = Formatting.Indented,
			ContractResolver = new SceneContractResolver(),
			Converters = { new PrimitiveCreationConverter() }
		};

		// Load an existing scene from disk.
		public static Scene LoadFromFile(string filename, ILoggerFactory loggerFactory)
		{
			// Any relative paths are relative to the scene file itself.
			// TODO: do this more elegantly.
			var prevCurrentDir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(filename);

			var logger = loggerFactory.CreateLogger("SceneLoad");

			try
			{
				// Load JSON file.
				var contents = File.ReadAllText(filename);
				var result = JsonConvert.DeserializeObject<Scene>(contents, _serializerSettings);

				result._filename = filename;

				result.PostLoad(logger);

				Environment.CurrentDirectory = prevCurrentDir;
				return result;
			}
			catch (IOException ex)
			{
				logger.LogLine("Failed to load scene {0}", filename);
				logger.LogLine(ex.Message);
				return null;
			}
			catch (JsonException ex)
			{
				logger.LogLine("Failed to parse scene {0}", filename);
				logger.LogLine(ex.Message);
				return null;
			}
			catch (Exception ex)
			{
				// Catch-all for any exception thrown during the parsing process in case of malformed scene.
				// TODO: Perhaps be more selective?
				logger.LogLine("Scene is invalid: {0}", filename);
				logger.LogLine(ex.Message);
				return null;
			}
		}

		public void Save()
		{
			// Serialise and write to file.
			var json = JsonConvert.SerializeObject(this, _serializerSettings);
			File.WriteAllText(_filename, json);
		}

		private void PostLoad(ILogger logger)
		{
			// Fix up mesh and material names after serialisation.
			foreach (var kvp in Meshes)
			{
				kvp.Value.Name = kvp.Key;
			}
			foreach (var kvp in Materials)
			{
				kvp.Value.Name = kvp.Key;
			}

			// Call PostLoad on sub-objects.
			Meshes.Values.ForEach(mesh => mesh.PostLoad(logger));
			Materials.Values.ForEach(mesh => mesh.PostLoad(logger));
			Primitives.ForEach(mesh => mesh.PostLoad(this, logger));

			// ReactiveList resets its observales post-serialisation, so need to set ours up again.
			InitObservables();
		}
	}

	class SceneContractResolver : CamelCasePropertyNamesContractResolver
	{
		protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
		{
			var contract = base.CreateDictionaryContract(objectType);

			// Do not camel-case dictionary keys.
			contract.DictionaryKeyResolver = name => name;

			return contract;
		}
	}

	class PrimitiveCreationConverter : PolymorphicJsonCreationConverter<Primitive>
	{
		protected override Primitive Create(JObject jObject)
		{
			var type = (string)jObject["type"];
			switch (type)
			{
				case "Sphere":
					return new SpherePrimitive();
				case "MeshInstance":
					return new MeshInstancePrimitive();
				case "Cube":
					return new SimplePrimitive(PrimitiveType.Cube);
				case "Plane":
					return new SimplePrimitive(PrimitiveType.Plane);
			}

			throw new JsonException("Unknown primitive type: " + type);
		}
	}
}
