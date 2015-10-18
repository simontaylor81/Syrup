using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
		};

		// Load an existing scene from disk.
		public static Scene LoadFromFile(string filename)
		{
			Scene result = new Scene();
			result.filename = filename;

			// Any relative paths are relative to the scene file itself.
			// TODO: do this more elegantly.
			var prevCurrentDir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(filename);

			try
			{
				// Load JSON file.
				JObject root;
				using (var reader = File.OpenText(filename))
				{
					root = JObject.Load(new JsonTextReader(reader));
				}

				// Load meshes.
				result.meshes = root["meshes"]
					.EmptyIfNull()
					.Select(obj => SceneMesh.Load(obj))
					.ToDictionary(mesh => mesh.Name);

				// Load materials.
				result.materials = root["materials"]
					.EmptyIfNull()
					.Select(obj => Material.Load(obj))
					.ToDictionary(mat => mat.Name);

				// Load primitives. Must be done after meshes and materials as primitives can refer to them.
				result.primitives.AddRange(root["primitives"]
					.EmptyIfNull()
					.Select(obj => result.CreatePrimitive(obj))
					.Where(prim => prim != null));

				// Load lights. Lights are completely semantic free to the app, they're just there
				// so the script can access them. Do we just convert the JSON directly to dynamic objects.
				result.lights = root["lights"]
					.EmptyIfNull()
					.Select(obj => DynamicHelpers.CreateDynamicObject(obj))
					.ToList();

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
			var root = new
			{
				meshes = Meshes.Values,
				materials = Materials.Values,
				primitives = Primitives,
			};

			// Serialise and write to file.
			var json = JsonConvert.SerializeObject(root, _serializerSettings);
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
	}

	//class MyContractResolver : CamelCasePropertyNamesContractResolver
	//{
	//	public new static readonly MyContractResolver Instance = new MyContractResolver();
 
	//	protected override 
	//}
}
