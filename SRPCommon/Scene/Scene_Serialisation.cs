﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SRPCommon.Util;

namespace SRPCommon.Scene
{
	// Implementation of load and save functions for scenes.
	// Split into a separate file simply for cleanliness.
	public partial class Scene
	{
		static private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
		{
			Formatting = Formatting.Indented,
			ContractResolver = new SceneContractResolver(),
			Converters = { new PrimitiveCreationConverter() }
		};

		// Load an existing scene from disk.
		public static Scene LoadFromFile(string filename)
		{
			// Any relative paths are relative to the scene file itself.
			// TODO: do this more elegantly.
			var prevCurrentDir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(filename);

			try
			{
				// Load JSON file.
				var contents = File.ReadAllText(filename);
				var result = JsonConvert.DeserializeObject<Scene>(contents, _serializerSettings);

				result.filename = filename;

				/*
				JObject root;
				using (var reader = File.OpenText(filename))
				{
					root = JObject.Load(new JsonTextReader(reader));
				}

				// Load meshes.
				//result.meshes = root["meshes"]
				//	.EmptyIfNull()
				//	.Select(obj => SceneMesh.Load(obj))
				//	.ToDictionary(mesh => mesh.Name);

				// TEMP
				result.meshes = JsonConvert.DeserializeObject<Dictionary<string, SceneMesh>>(root["meshes"].ToString());

				// Load materials.
				//result.materials = root["materials"]
				//	.EmptyIfNull()
				//	.Select(obj => Material.Load(obj))
				//	.ToDictionary(mat => mat.Name);

				// TEMP
				result.materials = JsonConvert.DeserializeObject<Dictionary<string, Material>>(root["materials"].ToString());

				// Load primitives. Must be done after meshes and materials as primitives can refer to them.
				//result.primitives.AddRange(root["primitives"]
				//	.EmptyIfNull()
				//	.Select(obj => result.CreatePrimitive(obj))
				//	.Where(prim => prim != null));

				result.primitives = JsonConvert.DeserializeObject<ReactiveUI.ReactiveList<Primitive>>(
					root["primitives"].ToString(), _serializerSettings);

				// Load lights. Lights are completely semantic free to the app, they're just there
				// so the script can access them. So we just convert the JSON directly to dynamic objects.
				//result.lights = root["lights"]
				//	.EmptyIfNull()
				//	.Select(obj => DynamicHelpers.CreateDynamicObject(obj))
				//	.ToList();

				result.lights = JsonConvert.DeserializeObject<List<dynamic>>(
					root["lights"].ToString(), new JsonDynamicObjectConverter());
				*/

				result.PostLoad();
				result.NotifyChanged();

				Environment.CurrentDirectory = prevCurrentDir;
				return result;
			}
			catch (IOException ex)
			{
				OutputLogger.Instance.LogLine(LogCategory.Log, "Failed to load scene {0}", filename);
				OutputLogger.Instance.LogLine(LogCategory.Log, ex.Message);
				return null;
			}
			catch (JsonReaderException ex)
			{
				OutputLogger.Instance.LogLine(LogCategory.Log, "Failed to parse scene {0}", filename);
				OutputLogger.Instance.LogLine(LogCategory.Log, ex.Message);
				return null;
			}
			catch (Exception ex)
			{
				// Catch-all for any exception thrown during the parsing process in case of malformed scene.
				// TODO: Perhaps be more selective?
				OutputLogger.Instance.LogLine(LogCategory.Log, "Scene is invalid: {0}", filename);
				OutputLogger.Instance.LogLine(LogCategory.Log, ex.Message);
				return null;
			}
		}

		public void Save()
		{
			// Serialise and write to file.
			var json = JsonConvert.SerializeObject(this, _serializerSettings);
			File.WriteAllText(filename + ".test", json);
		}

		// Create a new primitive of the appropriate type.
		private Primitive CreatePrimitive(JToken obj)
		{
			var type = (string)obj["type"];
			switch (type)
			{
				case "sphere":
					var sphere = new SpherePrimitive();
					sphere.Load(obj, this);
					return sphere;

				case "meshInstance":
					var meshPrim = new MeshInstancePrimitive();
					meshPrim.Load(obj, this);
					return meshPrim;
			}

			OutputLogger.Instance.LogLine(LogCategory.Log, "Unknown primitive type: " + type);
			return null;
		}

		private void PostLoad()
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
			Meshes.Values.ForEach(mesh => mesh.PostLoad());
			Primitives.ForEach(mesh => mesh.PostLoad(this));
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
			}

			throw new JsonException("Unknown primitive type: " + type);
		}
	}
}
