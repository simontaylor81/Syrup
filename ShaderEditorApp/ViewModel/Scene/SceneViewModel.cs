using System;
using System.Collections.Generic;
using System.Linq;
using ShaderEditorApp.Workspace;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.ViewModel;
using SRPCommon.UserProperties;
using ReactiveUI;
using System.Windows.Input;
using SRPCommon.Scene;
using System.Reactive.Linq;

namespace ShaderEditorApp.ViewModel.Scene
{
	public class SceneViewModel : ReactiveObject, IPropertySource, IHierarchicalBrowserRootViewModel
	{
		#region IPropertySource interface

		private ObservableAsPropertyHelper<IEnumerable<IUserProperty>> _properties;
		public IEnumerable<IUserProperty> Properties => _properties.Value;

		#endregion

		#region IHierarchicalBrowserRootViewModel interface

		private IHierarchicalBrowserNodeViewModel[] _rootNodes;
		public IEnumerable<IHierarchicalBrowserNodeViewModel> RootNodes => _rootNodes;

		private IHierarchicalBrowserNodeViewModel _activeItem;
		public IHierarchicalBrowserNodeViewModel ActiveItem
		{
			get { return _activeItem; }
			set { this.RaiseAndSetIfChanged(ref _activeItem, value); }
		}

		#endregion

		public SceneViewModel(SRPCommon.Scene.Scene scene)
		{
			_rootNodes = new[] { new ScenePrimitivesViewModel(scene) };

			_properties = this.WhenAny(x => x.ActiveItem, change => change.Value)
				.Where(node => node != null)
				.Select(node => node.UserProperties)
				.ToProperty(this, x => x.Properties);
		}
	}
}
