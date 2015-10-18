using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRPCommon.Util;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SRPCommon.Scene
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

		public string Filename => filename;
		public IEnumerable<Primitive> Primitives => primitives;
		public IDictionary<string, SceneMesh> Meshes => meshes;
		public IDictionary<string, Material> Materials => materials;

		// Observable that fires when something important changes in the scene.
		public IObservable<Unit> OnChanged { get { return _onChanged; } }
		private Subject<Unit> _onChanged = new Subject<Unit>();

		// Subscription to primitives' OnChanged observables.
		private IDisposable _onPrimitivesChangedSubscription;

		private string filename;
		private List<Primitive> primitives;
		private Dictionary<string, SceneMesh> meshes;
		private Dictionary<string, Material> materials;

		// Array of lights. Purely for access by script, so just dynamic objects.
		public IEnumerable<dynamic> Lights => lights;
		private List<dynamic> lights;

		public void AddPrimitive(Primitive primitive)
		{
			primitives.Add(primitive);
			NotifyChanged();
		}

		private void NotifyChanged()
		{
			if (_onPrimitivesChangedSubscription != null)
			{
				_onPrimitivesChangedSubscription.Dispose();
			}

			var primitivesChanged = Observable.Merge(Primitives.Select(p => p.OnChanged));
			_onPrimitivesChangedSubscription = primitivesChanged.Subscribe(_onChanged);

			//primitivesChanged.Subscribe(_ => System.Diagnostics.Debug.WriteLine("Scene prims changed"));
			//foreach (var prim in Primitives)
			//{
			//	prim.OnChanged.Subscribe(_ => System.Diagnostics.Debug.WriteLine("Scene prim changed"));
			//}

			// Fire an event for this change itself.
			_onChanged.OnNext(Unit.Default);
		}
	}
}
