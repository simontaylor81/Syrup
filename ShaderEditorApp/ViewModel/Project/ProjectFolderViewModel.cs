using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Windows;
using Microsoft.Win32;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.Workspace;
using ShaderEditorApp.ViewModel;
using SRPCommon.UserProperties;
using System.Windows.Input;
using ShaderEditorApp.Projects;

namespace ShaderEditorApp.ViewModel.Projects
{
	public class ProjectFolderViewModel : ViewModelBase, IHierarchicalBrowserNodeViewModel
	{
		public ProjectFolderViewModel(ProjectFolder folder, Project project, WorkspaceViewModel workspace)
		{
			this.folder = folder;
			Project = project;
			Workspace = workspace;

			// Build the list of child items.
			RegenerateChildren();

			// And hook up event to do so again when the underlying items change.
			((INotifyCollectionChanged)folder.SubFolders).CollectionChanged += (o, e) => RegenerateChildren();
			((INotifyCollectionChanged)folder.Items).CollectionChanged += (o, e) => RegenerateChildren();

			// Add commands.
			Commands = new NamedCommand[] { AddExistingCmd, AddNewCmd, RemoveCmd };

			// User-facing properties.
			var nameProp = new MutableScalarProperty<string>("Folder Name", folder.Name);
			nameProp.Subscribe(_ => DisplayName = nameProp.Value);

			UserProperties = new[] { nameProp };
		}

		private void RegenerateChildren()
		{
			IEnumerable<IHierarchicalBrowserNodeViewModel> subfolders = from subfolder in folder.SubFolders select new ProjectFolderViewModel(subfolder, Project, Workspace);
			IEnumerable<IHierarchicalBrowserNodeViewModel> items = from item in folder.Items select new ProjectItemViewModel(item, Project, Workspace);
			ProjectChildren = new ReadOnlyObservableCollection<IHierarchicalBrowserNodeViewModel>(
				new ObservableCollection<IHierarchicalBrowserNodeViewModel>(subfolders.Concat(items)));
		}

		// Get the name of the folder.
		public override string DisplayName
		{
			get { return folder.Name; }
			protected set
			{
				if (value != folder.Name)
				{
					folder.Name = value;
					OnPropertyChanged();
				}
			}
		}

		// Prompt the user to add select a file to add, then add it to the project.
		private void AddExistingFile()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = FileFilter;
			dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				folder.AddItem(dialog.FileName);
			}
		}

		// Add a new item to the project.
		private void AddNewFile()
		{
			var dialog = new SaveFileDialog();
			dialog.Filter = FileFilter;
			dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				// Create an empty file.
				File.WriteAllText(dialog.FileName, "");

				// Add it to the project.
				folder.AddItem(dialog.FileName);
			}
		}

		// Remove the entire folder from the project.
		private void RemoveFromProject()
		{
			// Ask the user if they're sure.
			var msg = String.Format("Remove '{0}' from project, including all sub items?", folder.Name);
			if (MessageBoxResult.OK == MessageBox.Show(msg, "SRP", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation))
			{
				folder.RemoveFromProject();
			}
		}

		// Get the file filter to use for the open dialog, based on the item type.
		private string FileFilter
		{
			get
			{
				return
					"Supported file types|*.hlsl;*.fx;*.py;*.srpscene" +
					"|Shader files|*.hlsl;*.fx" +
					"|Python files|*.py" +
					"|Scene files|*.srpscene" +
					"|All Files|*.*";
			}
		}

		#region IHierarchicalBrowserNodeViewModel interface

		/// <summary>
		/// Commnads that can be executed on this node (used for drop-down menu).
		/// </summary>
		public IEnumerable<ICommand> Commands { get; protected set; }

		/// <summary>
		/// Set of properties that this node exposes.
		/// </summary>
		public IEnumerable<IUserProperty> UserProperties { get; protected set; }

		// We don't have a default command.
		public ICommand DefaultCmd { get { return null; } }

		/// <summary>
		/// List of child nodes.
		/// </summary>
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children
		{
			get { return ProjectChildren; }
		}

		// We don't have a default folder.
		public bool IsDefault { get { return false; } }

		#endregion

		#region Commands

		// Command to open the project item into the workspace.
		private NamedCommand addExistingCmd;
		public NamedCommand AddExistingCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref addExistingCmd, "Add Existing File",
						(param) => AddExistingFile());
			}
		}

		// Command to open the project item into the workspace.
		private NamedCommand addNewCmd;
		public NamedCommand AddNewCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref addNewCmd, "Add New File",
						(param) => AddNewFile());
			}
		}

		// Command to remove the folder from the project.
		private NamedCommand removeCmd;
		public NamedCommand RemoveCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref removeCmd, "Remove",
						(param) => RemoveFromProject());
			}
		}

		#endregion

		protected Project Project { get; private set; }
		protected WorkspaceViewModel Workspace { get; private set; }
		private ProjectFolder folder;

		private ReadOnlyObservableCollection<IHierarchicalBrowserNodeViewModel> children_;
		public ReadOnlyObservableCollection<IHierarchicalBrowserNodeViewModel> ProjectChildren
		{
			get { return children_; }
			private set
			{
				if (children_ != value)
				{
					children_ = value;
					OnPropertyChanged();
					OnPropertyChanged("Children");
				}
			}
		}
	}
}
