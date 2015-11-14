using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using SRPCommon.UserProperties;
using SRPCommon.Scene;
using ShaderEditorApp.MVVMUtil;

namespace ShaderEditorApp.ViewModel.Scene
{
	/// <summary>
	/// Parent node that contains the list of primitives in the scene.
	/// </summary>
	class ScenePrimitivesViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		#region IHierarchicalBrowserNodeViewModel interface

		public string DisplayName => "Primitives";

		private SRPCommon.Scene.Scene Scene { get; }

		public IEnumerable<object> MenuItems { get; }

		public IEnumerable<IUserProperty> UserProperties => Enumerable.Empty<IUserProperty>();

		private IReactiveDerivedList<ScenePrimitiveViewModel> _children;
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => _children;

		public ICommand DefaultCmd => null;

		public bool IsDefault => false;

		#endregion

		public ScenePrimitivesViewModel(SRPCommon.Scene.Scene scene)
		{
			Scene = scene;

			_children = scene.Primitives.CreateDerivedCollection(prim => ScenePrimitiveViewModel.Create(prim, scene));

			// Create commands.
			MenuItems = new[]
			{
				new CommandMenuItem(new CommandViewModel("Add Cube", CommandUtil.Create(_ => AddCube()))),
				new CommandMenuItem(new CommandViewModel("Add Sphere", CommandUtil.Create(_ => AddSphere()))),
				new CommandMenuItem(new CommandViewModel("Add Plane", CommandUtil.Create(_ => AddPlane()))),
				new CommandMenuItem(new CommandViewModel("Save", CommandUtil.Create(_ => scene.Save())))
			};
		}

		private void AddCube()
		{
			var cube = new SimplePrimitive(PrimitiveType.Cube);
			Scene.AddPrimitive(cube);
		}

		private void AddSphere()
		{
			var sphere = new SpherePrimitive();
			Scene.AddPrimitive(sphere);
		}

		private void AddPlane()
		{
			var plane = new SimplePrimitive(PrimitiveType.Plane);
			Scene.AddPrimitive(plane);
		}
	}
}
