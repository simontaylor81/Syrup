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

		public string DisplayName { get { return "Primitives"; } }

		private SRPCommon.Scene.Scene Scene { get; set; }

		public IEnumerable<ICommand> Commands { get; private set; }

		public IEnumerable<IUserProperty> UserProperties
		{
			get { return Enumerable.Empty<IUserProperty>(); }
		}

		private IHierarchicalBrowserNodeViewModel[] _children;
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children
		{
			get { return _children; }
		}

		public ICommand DefaultCmd
		{
			get { return null; }
		}

		public bool IsDefault { get { return false; } }

		#endregion

		public ScenePrimitivesViewModel(SRPCommon.Scene.Scene scene)
		{
			Scene = scene;
			_children = scene.Primitives.Select(prim => ScenePrimitiveViewModel.Create(prim)).ToArray();

			// Create commands.
			Commands = new[]
			{
				NamedCommand.CreateReactive("Add Sphere", _ => AddSphere())
			};
		}

		private void AddSphere()
		{
			var sphere = new SpherePrimitive();
			Scene.AddPrimitive(sphere);
		}
	}
}
