using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShaderEditorApp.Scene
{
	// Representation of the scene that is to be rendered.
	public class Scene
	{
		// Load an existing scene from disk.
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
