using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using SRPCommon.UserProperties;
using SRPCommon.Scene;

namespace ShaderEditorApp.ViewModel.Scene
{
	/// <summary>
	/// Parent node that contains the list of primitives in the scene.
	/// </summary>
	class ScenePrimitivesViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		#region IHierarchicalBrowserNodeViewModel interface

		public string DisplayName { get { return "Primitives"; } }

		public IEnumerable<ICommand> Commands
		{
			get { return Enumerable.Empty<ICommand>(); }
		}

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

		public ScenePrimitivesViewModel(IEnumerable<Primitive> primitives)
		{
			_children = primitives.Select(prim => ScenePrimitiveViewModel.Create(prim)).ToArray();
		}
	}
}
