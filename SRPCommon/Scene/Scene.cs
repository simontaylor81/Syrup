using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace SRPCommon.Scene
{
	// Representation of the scene that is to be rendered.
	public partial class Scene
	{
		public string Filename => filename;
		public IEnumerable<Primitive> Primitives => primitives;
		public IDictionary<string, SceneMesh> Meshes => meshes;
		public IDictionary<string, Material> Materials => materials;

		// Observable that fires when something important changes in the scene.
		public IObservable<Unit> OnChanged => _onChanged;
		private Subject<Unit> _onChanged = new Subject<Unit>();

		// Subscription to primitives' OnChanged observables.
		private IDisposable _onPrimitivesChangedSubscription;

		private string filename;
		private ReactiveList<Primitive> primitives = new ReactiveList<Primitive>();
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

			// Fire an event for this change itself.
			_onChanged.OnNext(Unit.Default);
		}
	}
}
