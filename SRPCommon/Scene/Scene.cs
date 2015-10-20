using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Newtonsoft.Json;
using ReactiveUI;
using SRPCommon.Util;

namespace SRPCommon.Scene
{
	// Representation of the scene that is to be rendered.
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public partial class Scene
	{
		public string Filename => _filename;
		public IEnumerable<Primitive> Primitives => _primitives;
		public IDictionary<string, SceneMesh> Meshes => _meshes;
		public IDictionary<string, Material> Materials => _materials;

		// Array of lights. Purely for access by script, so just dynamic objects.
		public IEnumerable<dynamic> Lights => _lights;

		// Observable that fires when something important changes in the scene.
		public IObservable<Unit> OnChanged { get; private set; }
		
		private string _filename;

		[JsonProperty("primitives")]
		private ReactiveList<Primitive> _primitives = new ReactiveList<Primitive>();

		[JsonProperty("meshes")]
		private Dictionary<string, SceneMesh> _meshes = new Dictionary<string, SceneMesh>();

		[JsonProperty("materials")]
		private Dictionary<string, Material> _materials = new Dictionary<string, Material>();

		[JsonProperty("lights", ItemConverterType = typeof(JsonDynamicObjectConverter))]
		private List<dynamic> _lights = new List<dynamic>();

		public Scene()
		{
			InitObservables();
		}

		public void AddPrimitive(Primitive primitive)
		{
			_primitives.Add(primitive);
		}

		private void InitObservables()
		{
			var primitiveChanged = _primitives.Changed									// When the primitive list changes...
				.Select(evt => _primitives)												// get the list of primitives...
				.Select(primList => primList.Select(prim => prim.OnChanged).Merge())	// merge all the primitive notifications...
				.Switch();                                                              // and take the most recent set.

			// We change when a primitive changes, or the primitive list changes.
			OnChanged = Observable.Merge(primitiveChanged, _primitives.Changed.Select(_ => Unit.Default));
		}
	}
}
