using System;
using System.Collections.Generic;
using System.Linq;
using ShaderEditorApp.Workspace;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.ViewModel;
using SRPCommon.UserProperties;
using ShaderEditorApp.Projects;
using ReactiveUI;
using System.Reactive.Linq;

namespace ShaderEditorApp.ViewModel.Projects
{
	// View-model class for the project itself. Derives from the folder type, which is used to display the root node.
	public class ProjectViewModel : ProjectFolderViewModel, IPropertySource, IHierarchicalBrowserRootViewModel
	{
		public ProjectViewModel(Project project, WorkspaceViewModel workspace)
			: base(project.RootFolder, project, workspace)
		{
			// Can't remove the project itself, but you can save it.
			Commands = new [] { SaveCmd, AddExistingCmd, AddNewCmd, AddSubFolder };

			// We don't have any properties of our own.
			UserProperties = null;

			_displayName = this.WhenAnyObservable(x => x.Project.IsDirtyObservable)
				.Select(isDirty => GetDisplayName(isDirty))
				.ToProperty(this, x => x.DisplayName, GetDisplayName(Project.IsDirty));

			_properties = this.WhenAnyValue(x => x.ActiveItem)
				.Select(activeItem => GetProperties(activeItem))
				.ToProperty(this, x => x.Properties);
		}

		private string GetDisplayName(bool isDirty)
		{
			var result = Project.Name;
			if (isDirty)
				result += "*";
			return result;
		}

		private IEnumerable<IUserProperty> GetProperties(IHierarchicalBrowserNodeViewModel activeItem)
		{
			if (ActiveItem != null)
				return ActiveItem.UserProperties;
			else
				return UserProperties;
		}

		// Properties to display for the currently selected property item.
		private ObservableAsPropertyHelper<IEnumerable<IUserProperty>> _properties;
		public IEnumerable<IUserProperty> Properties { get { return _properties.Value; } }

		// Currently selected node in the project.
		private IHierarchicalBrowserNodeViewModel activeItem;
		public IHierarchicalBrowserNodeViewModel ActiveItem
		{
			get { return activeItem; }
			set { this.RaiseAndSetIfChanged(ref activeItem, value); }
		}

		private void Save()
		{
			// Set list of open documents so they can be restored next time.
			Project.SavedOpenDocuments = from doc in Workspace.Documents select doc.FilePath;

			Project.Save();
		}

		// Allow this object to be used as a single root node of the tree (since the tree control needs a list of items).
		public IEnumerable<IHierarchicalBrowserNodeViewModel> RootNodes { get { return new [] { this }; } }

		// Command to save the project.
		private NamedCommand saveCmd;
		public NamedCommand SaveCmd
		{
			get
			{
				if (saveCmd == null)
				{
					saveCmd = new NamedCommand("Save", new RelayCommand(
						(param) => Save()
						//(param) => Project.IsDirty
						));
				}
				return saveCmd;
			}
		}
	}
}
