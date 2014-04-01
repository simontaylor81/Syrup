using System;
using System.Collections.Generic;
using System.Linq;
using ShaderEditorApp.Workspace;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.ViewModel;
using SRPCommon.UserProperties;
using ShaderEditorApp.Projects;

namespace ShaderEditorApp.ViewModel.Projects
{
	// View-model class for the project itself. Derives from the folder type, which is used to display the root node.
	public class ProjectViewModel : ProjectFolderViewModel, IPropertySource, IHierarchicalBrowserRootViewModel
	{
		public ProjectViewModel(Project project, WorkspaceViewModel workspace)
			: base(project.RootFolder, project, workspace)
		{
			// Can't remove the project itself, but you can save it.
			Commands = new [] { SaveCmd, AddExistingCmd, AddNewCmd };

			Project.DirtyChanged += OnDirtyChanged;

			// We don't have any properties of our own.
			UserProperties = null;
		}

		protected override void OnDispose()
		{
			Project.DirtyChanged -= OnDirtyChanged;
			base.OnDispose();
		}

		public override string DisplayName
		{
			get
			{
				var result = Project.Name;
				if (Project.IsDirty)
					result += "*";
				return result;
			}
		}

		// Properties to display for the currently selected property item.
		public IEnumerable<IUserProperty> Properties
		{
			get
			{
				if (ActiveItem != null)
					return ActiveItem.UserProperties;
				else
					return UserProperties;
			}
		}

		// Currently selected node in the project.
		private IHierarchicalBrowserNodeViewModel activeItem;
		public IHierarchicalBrowserNodeViewModel ActiveItem
		{
			get { return activeItem; }
			set
			{
				if (value != activeItem)
				{
					activeItem = value;
					OnPropertyChanged();

					// Properties are dependent on the active item.
					OnPropertyChanged("Properties");
				}
			}
		}

		private void Save()
		{
			// Set list of open documents so they can be restored next time.
			Project.SavedOpenDocuments = from doc in Workspace.Documents select doc.FilePath;

			Project.Save();
		}

		// Allow this object to be used as a single root node of the tree (since the tree control needs a list of items).
		public IEnumerable<ProjectFolderViewModel> RootNodes { get { return new [] { this }; } }

		// Called when the project's dirty state changes.
		void OnDirtyChanged()
		{
			// Display name has an asterisk after it if the project is dirty.
			OnPropertyChanged("DisplayName");
		}

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
