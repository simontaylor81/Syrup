using ReactiveUI;
using SlimDX;
using SRPCommon.Scene;
using SRPCommon.UserProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Windows.Input;

namespace ShaderEditorApp.ViewModel.Scene
{
	abstract class ScenePrimitiveViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		private Primitive _primitive;
		private CompositeDisposable _disposables;

		#region IHierarchicalBrowserNodeViewModel interface

		public abstract string DisplayName { get; }

		public IEnumerable<ICommand> Commands
		{
			get { return Enumerable.Empty<ICommand>(); }
		}

		private IEnumerable<IUserProperty> _userProperties;
		public IEnumerable<IUserProperty> UserProperties
		{
			get { return _primitive.UserProperties; }
		}

		// No children
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children { get { return null; } }

		public ICommand DefaultCmd
		{
			get { return null; }
		}

		public bool IsDefault { get { return false; } }

		#endregion

		public static ScenePrimitiveViewModel Create(Primitive primitive)
		{
			var mesh = primitive as MeshInstancePrimitive;
			var sphere = primitive as SpherePrimitive;

			if (mesh != null)
			{
				return new MeshInstancePrimitiveViewModel(mesh);
			}
			else if (sphere != null)
			{
				return new SpherePrimitiveViewModel(sphere);
			}
			throw new ArgumentException("Unknown primitive type");
		}

		protected ScenePrimitiveViewModel(Primitive primitive)
		{
			_primitive = primitive;

			//_userProperties = new[] {
			//	new Vector3Property(
			//};
		}

		//protected IUserProperty CreateVectorProperty(string name, Vector3 value, Action<Vector3> setter)
		//{
		//	var property = new Vector3Property(name, value);
		//	_disposables.Add(property.Subscribe(_ => setter(property
		//}
	}
}
