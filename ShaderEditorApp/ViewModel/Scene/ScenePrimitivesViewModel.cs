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

		public IEnumerable<ICommand> Commands { get; }

		public IEnumerable<IUserProperty> UserProperties => Enumerable.Empty<IUserProperty>();

		private IReactiveDerivedList<ScenePrimitiveViewModel> _children;
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => _children;

		public ICommand DefaultCmd => null;

		public bool IsDefault => false;

		#endregion

		public ScenePrimitivesViewModel(SRPCommon.Scene.Scene scene)
		{
			Scene = scene;

			_children = scene.Primitives.CreateDerivedCollection(prim => ScenePrimitiveViewModel.Create(prim));

			// Create commands.
			Commands = new[]
			{
				NamedCommand.CreateReactive("Add Sphere", _ => AddSphere()),
				NamedCommand.CreateReactive("Save", _ => scene.Save())
			};
		}

		private void AddSphere()
		{
			var sphere = new SpherePrimitive();
			Scene.AddPrimitive(sphere);
		}
	}
}
