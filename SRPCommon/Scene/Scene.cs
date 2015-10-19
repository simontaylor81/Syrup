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
		public string Filename => filename;
		public IEnumerable<Primitive> Primitives => primitives;
		public IDictionary<string, SceneMesh> Meshes => meshes;
		public IDictionary<string, Material> Materials => materials;

		// Array of lights. Purely for access by script, so just dynamic objects.
		public IEnumerable<dynamic> Lights => lights;

		// Observable that fires when something important changes in the scene.
		public IObservable<Unit> OnChanged => _onChanged;
		private Subject<Unit> _onChanged = new Subject<Unit>();

		// Subscription to primitives' OnChanged observables.
		private IDisposable _onPrimitivesChangedSubscription;

		private string filename;

		[JsonProperty]
		private ReactiveList<Primitive> primitives = new ReactiveList<Primitive>();

		[JsonProperty]
		private Dictionary<string, SceneMesh> meshes = new Dictionary<string, SceneMesh>();

		[JsonProperty]
		private Dictionary<string, Material> materials = new Dictionary<string, Material>();

		[JsonProperty(ItemConverterType = typeof(JsonDynamicObjectConverter))]
		private List<dynamic> lights = new List<dynamic>();

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

			// Fire an event for this change itself.
			_onChanged.OnNext(Unit.Default);
		}
	}
}
