using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ShaderEditorApp.Util;

namespace ShaderEditorApp.Scene
{
	// Representation of the scene that is to be rendered.
	public class Scene
	{
		// Load an existing scene from disk.
		/*
		public static Scene LoadFromFile(string filename)
		{
			Scene result = new Scene();
			result.filename = filename;

			// Any relative paths are relative to the scene file itself.
			var prevCurrentDir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName(filename);

			// Load the xml file.
			var xdoc = XDocument.Load(filename);

			// Load meshes.
			result.meshes = (from element in xdoc.Descendants("meshes").First().Descendants("mesh") select SceneMesh.Load(element))
				.ToDictionary(mesh => mesh.Name);

			// Load materials.
			result.materials = (from element in xdoc.Descendants("materials").First().Descendants("material") select Material.Load(element))
				.ToDictionary(mat => mat.Name);

			// Load primitives. Must be done after meshes and materials as primitives can refer to them.
			var primitives = from element in xdoc.Descendants("primitives").First().Descendants() select result.CreatePrimitive(element);
			result.primitives = (from primitive in primitives where primitive != null select primitive).ToList();

			Environment.CurrentDirectory = prevCurrentDir;
			return result;
		}
		*/

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
				result.primitives = root["primitives"]
					.EmptyIfNull()
					.Select(obj => result.CreatePrimitive(obj))
					.Where(prim => prim != null)
					.ToList();

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

		private Primitive CreatePrimitive(XElement element)
		{
			switch (element.Name.LocalName)
			{
				case "sphere":
					var sphere = new SpherePrimitive();
					sphere.Load(element, this);
					return sphere;

				case "meshInstance":
					var meshPrim = new MeshInstancePrimitive();
					meshPrim.Load(element, this);
					return meshPrim;
			}

			OutputLogger.Instance.LogLine(LogCategory.Log, "Unknown primitive type: " + element.Name.LocalName);
			return null;
		}

		public string Filename { get { return filename; } }
		public IEnumerable<Primitive> Primitives { get { return primitives; } }
		public IDictionary<string, SceneMesh> Meshes { get { return meshes; } }
		public IDictionary<string, Material> Materials { get { return materials; } }

		private string filename;
		private List<Primitive> primitives;
		private Dictionary<string, SceneMesh> meshes;
		private Dictionary<string, Material> materials;
	}
}
