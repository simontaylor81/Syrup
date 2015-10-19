﻿using System;
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
using ReactiveUI;
using System.Reactive.Linq;

namespace ShaderEditorApp.ViewModel.Projects
{
	public class ProjectFolderViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		public ProjectFolderViewModel(ProjectFolder folder, Project project, WorkspaceViewModel workspace)
		{
			this.Folder = folder;
			Project = project;
			Workspace = workspace;

			// Build the list of child items.
			CreateChildrenProperty();

			AddSubFolder = NamedCommand.CreateReactive("Add Folder", _ => folder.AddFolder("NewFolder"));

			// Add commands.
			Commands = new NamedCommand[] { AddExistingCmd, AddNewCmd, AddNewSceneCmd, AddSubFolder, RemoveCmd };

			// User-facing properties.
			var nameProp = new MutableScalarProperty<string>("Folder Name", folder.Name);
			nameProp.Subscribe(_ => folder.Name = nameProp.Value);
			_displayName = nameProp
				.Select(_ => nameProp.Value)
				.ToProperty(this, x => x.DisplayName, folder.Name);

			UserProperties = new[] { nameProp };
		}

		private void CreateChildrenProperty()
		{
			IReactiveDerivedList<IHierarchicalBrowserNodeViewModel> subfolderViewModels
				= Folder.SubFolders.CreateDerivedCollection(subfolder => new ProjectFolderViewModel(subfolder, Project, Workspace));
			IReactiveDerivedList<IHierarchicalBrowserNodeViewModel> itemViewModels
				= Folder.Items.CreateDerivedCollection(item => new ProjectItemViewModel(item, Project, Workspace));

			_children = Observable.Merge(subfolderViewModels.Changed, itemViewModels.Changed)
				.Select(_ => subfolderViewModels.Concat(itemViewModels))
				.ToProperty(this, x => x.Children, subfolderViewModels.Concat(itemViewModels));
		}

		// Get the name of the folder.
		protected ObservableAsPropertyHelper<string> _displayName;
		public string DisplayName => _displayName.Value;

		// Prompt the user to add select a file to add, then add it to the project.
		private void AddExistingFile()
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = FileFilterExisting;
			dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				AddItem(dialog.FileName);
			}
		}

		// Add a new item to the project.
		private void AddNewFile()
		{
			var dialog = new SaveFileDialog();
			dialog.Filter = FileFilterNew;
			dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				// Create an empty file.
				File.WriteAllText(dialog.FileName, "");

				// Add it to the project.
				AddItem(dialog.FileName);
			}
		}

		// Add a new scene file to the project.
		private void AddNewScene()
		{
			var dialog = new SaveFileDialog();
			dialog.Filter = "Scene files|*.srpscene";
			dialog.InitialDirectory = Project.BasePath;

			var result = dialog.ShowDialog();
			if (result == true)
			{
				// Create an empty scene file.
				File.WriteAllText(dialog.FileName, "{}");

				AddItem(dialog.FileName);
			}
		}

		private void AddItem(string filename)
		{
			// Add it to the project.
			var item = Folder.AddItem(filename);

			// Special handling for scene files.
			if (item.Type == ProjectItemType.Scene)
			{
				// If we don't have a default scene, make this the default.
				if (Project.DefaultScene == null)
				{
					Project.DefaultScene = item;
				}

				// If we don't have a scene loaded, load the new one.
				if (!Workspace.HasCurrentScene)
				{
					Workspace.SetCurrentScene(filename);
				}
			}
		}

		// Add the given file to the project.
		public void AddFile(string path)
		{
			Folder.AddItem(path);
		}

		// Can this folder be moved to the given folder?
		public bool CanMoveTo(ProjectFolderViewModel dest) => Folder.CanMoveTo(dest.Folder);

		public void MoveTo(ProjectFolderViewModel dest)
		{
			Folder.MoveTo(dest.Folder);
		}

		// Remove the entire folder from the project.
		private void RemoveFromProject()
		{
			// Ask the user if they're sure.
			var msg = String.Format("Remove '{0}' from project, including all sub items?", Folder.Name);
			if (MessageBoxResult.OK == MessageBox.Show(msg, "SRP", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation))
			{
				Folder.RemoveFromProject();
			}
		}

		// Get the file filter to use for the open dialog.
		private static string FileFilterExisting
			=> "Supported file types|*.hlsl;*.fx;*.py;*.srpscene" +
				"|Shader files|*.hlsl;*.fx" +
				"|Python files|*.py" +
				"|Scene files|*.srpscene" +
				"|All Files|*.*";

		// Get the file filter to use for the new dialog (excludes scene files).
		private static string FileFilterNew
			=> "Supported file types|*.hlsl;*.fx;*.py" +
				"|Shader files|*.hlsl;*.fx" +
				"|Python files|*.py" +
				"|All Files|*.*";

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
		public ICommand DefaultCmd => null;

		/// <summary>
		/// List of child nodes.
		/// </summary>
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => _children.Value;
		private ObservableAsPropertyHelper<IEnumerable<IHierarchicalBrowserNodeViewModel>> _children;

		// We don't have a default folder.
		public bool IsDefault => false;

		#endregion

		#region Commands

		// Command to add an existing file from disk to the project.
		private NamedCommand addExistingCmd;
		public NamedCommand AddExistingCmd
			=> NamedCommand.LazyInit(ref addExistingCmd, "Add Existing File", (param) => AddExistingFile());

		// Command to add a new file to the project.
		private NamedCommand addNewCmd;
		public NamedCommand AddNewCmd
			=> NamedCommand.LazyInit(ref addNewCmd, "Add New File", (param) => AddNewFile());

		// Command to add a new scene to the project.
		private NamedCommand addNewSceneCmd;
		public NamedCommand AddNewSceneCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref addNewSceneCmd, "Add New Scene",
						(param) => AddNewScene());
			}
		}

		// Command to add a subfolder to this folder.
		public NamedCommand AddSubFolder { get; }

		// Command to remove the folder from the project.
		private NamedCommand removeCmd;
		public NamedCommand RemoveCmd
			=> NamedCommand.LazyInit(ref removeCmd, "Remove", (param) => RemoveFromProject());

		#endregion

		protected Project Project { get; }
		protected WorkspaceViewModel Workspace { get; }
		internal ProjectFolder Folder { get; }
	}
}
