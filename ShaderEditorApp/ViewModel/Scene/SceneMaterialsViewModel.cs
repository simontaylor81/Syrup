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
	class SceneMaterialsViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		#region IHierarchicalBrowserNodeViewModel interface

		public string DisplayName => "Materials";

		private SRPCommon.Scene.Scene Scene { get; }
		public IEnumerable<object> MenuItems => Enumerable.Empty<object>();
		public IEnumerable<IUserProperty> UserProperties => Enumerable.Empty<IUserProperty>();

		public ICommand DefaultCmd => null;
		public bool IsDefault => false;

		// TODO: Handle adding/removing children.
		private List<SceneMaterialViewModel> _children;
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => _children;

		#endregion

		public SceneMaterialsViewModel(SRPCommon.Scene.Scene scene)
		{
			Scene = scene;

			_children = scene.Materials.Values.Select(mat => new SceneMaterialViewModel(mat)).ToList();
		}
	}
}
