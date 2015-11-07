using System;
using System.Collections.Generic;
using System.Linq;
using SRPCommon.UserProperties;
using ReactiveUI;
using System.Reactive.Linq;

namespace ShaderEditorApp.ViewModel.Scene
{
	public class SceneViewModel : ReactiveObject, IPropertySource, IHierarchicalBrowserRootViewModel
	{
		#region IPropertySource interface

		private readonly ObservableAsPropertyHelper<IEnumerable<IUserProperty>> _properties;
		public IEnumerable<IUserProperty> Properties => _properties.Value;

		#endregion

		#region IHierarchicalBrowserRootViewModel interface

		public IEnumerable<IHierarchicalBrowserNodeViewModel> RootNodes { get; }

		private IHierarchicalBrowserNodeViewModel _activeItem;
		public IHierarchicalBrowserNodeViewModel ActiveItem
		{
			get { return _activeItem; }
			set { this.RaiseAndSetIfChanged(ref _activeItem, value); }
		}

		#endregion

		public SceneViewModel(SRPCommon.Scene.Scene scene)
		{
			RootNodes = new IHierarchicalBrowserNodeViewModel[]
			{
				new ScenePrimitivesViewModel(scene),
				new SceneMaterialsViewModel(scene)
			};

			_properties = this.WhenAny(x => x.ActiveItem, change => change.Value)
				.Where(node => node != null)
				.Select(node => node.UserProperties)
				.ToProperty(this, x => x.Properties);
		}
	}
}
