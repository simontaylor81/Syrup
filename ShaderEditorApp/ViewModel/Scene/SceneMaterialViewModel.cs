using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SRPCommon.Scene;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.ViewModel.Scene
{
	public class SceneMaterialViewModel : IHierarchicalBrowserNodeViewModel
	{
		private readonly Material _mat;

		public string DisplayName => _mat.Name;

		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => null;
		public IEnumerable<object> MenuItems => Enumerable.Empty<object>();
		public ICommand DefaultCmd => null;
		public bool IsDefault => false;

		public IEnumerable<IUserProperty> UserProperties => _mat.UserProperties;

		public SceneMaterialViewModel(Material mat)
		{
			_mat = mat;
		}
	}
}