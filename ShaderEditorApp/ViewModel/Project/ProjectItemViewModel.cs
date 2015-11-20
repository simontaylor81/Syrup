using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using SRPCommon.UserProperties;
using System.Windows.Input;
using ShaderEditorApp.Projects;
using ReactiveUI;
using System.Reactive.Linq;
using ShaderEditorApp.MVVMUtil;
using System.Reactive;
using SRPCommon.Util;

namespace ShaderEditorApp.ViewModel.Projects
{
	public class ProjectItemViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		public string DisplayName => item.Name;

		public ProjectItemViewModel(ProjectItem item, Project project, WorkspaceViewModel workspaceVM)
		{
			this.item = item;
			this.project = project;
			this._workspaceVM = workspaceVM;

			_isDefault = project.DefaultSceneChanged
				.Select(_ => item.IsDefault)
				.ToProperty(this, x => x.IsDefault, item.IsDefault);

			CreateCommands();
			CreateUserProperties();
		}

		private void CreateCommands()
		{
			// Create commands.
			Open = CommandUtil.Create(_ => _workspaceVM.OpenDocumentSet.OpenDocument(item.AbsolutePath, false));
			SetAsCurrent = CommandUtil.Create(_ => _workspaceVM.Workspace.SetCurrentScene(item.AbsolutePath));
			Remove = CommandUtil.Create(_ => RemoveFromProject());

			Run = ReactiveCommand.CreateAsyncTask(
				_workspaceVM.Workspace.CanRunScript,
				_ => _workspaceVM.Workspace.RunScriptFile(item.AbsolutePath));

			// Build list context menu items.
			var menuItems = new List<object>();

			if (item.Type == ProjectItemType.Scene)
			{
				// Scenes are set as current.
				menuItems.Add(new CommandMenuItem(new CommandViewModel("Set as Current", SetAsCurrent)));
			}
			else
			{
				// Everything else is just opened.
				menuItems.Add(new CommandMenuItem(new CommandViewModel("Open", Open)));
			}

			// Everything can be removed.
			menuItems.Add(new CommandMenuItem(new CommandViewModel("Remove", Remove)));

			if (item.Type == ProjectItemType.Script)
			{
				// Scripts can be run.
				menuItems.Add(new CommandMenuItem(new CommandViewModel("Run", Run)));
			}

			MenuItems = menuItems;
		}

		// Create the user properties to display for the item.
		private void CreateUserProperties()
		{
			var properties = new List<IUserProperty>();

			// Add some read-only properties.
			properties.Add(new ReadOnlyScalarProperty<string>("Filename", DisplayName));
			properties.Add(new ReadOnlyScalarProperty<string>("Full path", item.AbsolutePath));
			properties.Add(new ReadOnlyScalarProperty<string>("Type", ItemTypeString));

			if (item.Type == ProjectItemType.Script)
			{
				// Add a 'run on start-up' property for script items.
				var runOnStartupProp = new MutableScalarProperty<bool>("Run at Startup", item.RunAtStartup);
				runOnStartupProp.Subscribe(_ => item.RunAtStartup = runOnStartupProp.Value);
				properties.Add(runOnStartupProp);
			}

			UserProperties = properties;
		}

		// Get type of item as a string.
		public string ItemTypeString => Enum.GetName(typeof(ProjectItemType), item.Type);

		#region IHierarchicalBrowserNodeViewModel interface

		// Menu items for the drop-down menu.
		public IEnumerable<object> MenuItems { get; protected set; }

		/// <summary>
		/// Set of properties that this node exposes.
		/// </summary>
		public IEnumerable<IUserProperty> UserProperties { get; private set; }

		// 'Default' command -- i.e. the one to execute when double clicking on the item.
		// Default command for scenes is to open it and make it the current scene for the workspace.
		public ICommand DefaultCmd => item.Type == ProjectItemType.Scene ? SetAsCurrent : Open;

		// We don't have any children -- we're a leaf node.
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children => null;

		// Is this item the defaut of its type?
		private readonly ObservableAsPropertyHelper<bool> _isDefault;
		public bool IsDefault => _isDefault.Value;

		#endregion

		// Commands.
		public ReactiveCommand<object> Open { get; private set; }
		public ReactiveCommand<object> SetAsCurrent { get; private set; }
		public ReactiveCommand<object> Remove { get; private set; }
		public ReactiveCommand<Unit> Run { get; private set; }

		public bool CanMoveTo(ProjectFolderViewModel targetFolder) => item.CanMoveTo(targetFolder.Folder);

		public void MoveTo(ProjectFolderViewModel targetFolder)
		{
			item.MoveTo(targetFolder.Folder);
		}

		private void RemoveFromProject()
		{
			// Ask the user if they're sure.
			var msg = String.Format("Remove '{0}' from project?", item.Name);
			if (MessageBoxResult.OK == MessageBox.Show(msg, "SRP", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation))
			{
				item.RemoveFromProject();
			}
		}

		private ProjectItem item;
		private Project project;
		private WorkspaceViewModel _workspaceVM;
	}
}
