using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using ShaderEditorApp.ViewModel;
using ShaderEditorApp.Workspace;
using SRPCommon.UserProperties;
using System.Windows.Input;
using ShaderEditorApp.Projects;
using ReactiveUI;
using System.Reactive.Linq;
using ShaderEditorApp.MVVMUtil;

namespace ShaderEditorApp.ViewModel.Projects
{
	public class ProjectItemViewModel : ReactiveObject, IHierarchicalBrowserNodeViewModel
	{
		public string DisplayName { get { return item.Name; } }

		public ProjectItemViewModel(ProjectItem item, Project project, WorkspaceViewModel workspace)
		{
			this.item = item;
			this.project = project;
			this.workspace = workspace;

			_isDefault = project.DefaultSceneChanged
				.Select(_ => item.IsDefault)
				.ToProperty(this, x => x.IsDefault, item.IsDefault);

			CreateCommands();
			CreateUserProperties();
		}

		private void CreateCommands()
		{
			// Create appropriate default command.
			if (item.Type == ProjectItemType.Scene)
			{
				// Default command for scenes is to open it and make it the current scene for the workspace.
				_defaultCmd = NamedCommand.CreateReactive("Set as Current", _ => workspace.SetCurrentScene(item.AbsolutePath));
			}
			else
			{
				// Default for everything else is to open the item.s
				_defaultCmd = NamedCommand.CreateReactive("Open", _ => workspace.OpenDocument(item.AbsolutePath, false));
			}

			// Command to remove the item from the scene.
			var removeCmd = NamedCommand.CreateReactive("Remove", _ => RemoveFromProject());

			// Default set of commands for an item.
			var commands = new List<ICommand>
				{
					_defaultCmd,
					removeCmd,
				};

			if (item.Type == ProjectItemType.Script)
			{
				// Scripts can be run.
				commands.Add(NamedCommand.CreateReactive(
					"Run",
					_ => workspace.RunScriptFile(item.AbsolutePath)));
			}
			else if (item.Type == ProjectItemType.Scene)
			{
				// Scenes can be set as the default scene.
				commands.Add(NamedCommand.CreateReactive(
					"Set as default",
					_ => item.SetAsDefault()));
			}

			Commands = commands;
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
		public string ItemTypeString { get { return Enum.GetName(typeof(ProjectItemType), item.Type); } }

		#region IHierarchicalBrowserNodeViewModel interface

		/// <summary>
		/// Commnads that can be executed on this node (used for drop-down menu).
		/// </summary>
		public IEnumerable<ICommand> Commands { get; private set; }

		/// <summary>
		/// Set of properties that this node exposes.
		/// </summary>
		public IEnumerable<IUserProperty> UserProperties { get; private set; }

		// 'Default' command -- i.e. the one to execute when double clicking on the item.
		public ICommand DefaultCmd { get { return _defaultCmd; } }
		private NamedCommand _defaultCmd;

		// We don't have any children -- we're a leaf node.
		public IEnumerable<IHierarchicalBrowserNodeViewModel> Children { get { return null; } }

		// Is this item the defaut of its type?
		private readonly ObservableAsPropertyHelper<bool> _isDefault;
		public bool IsDefault { get { return _isDefault.Value; } }

		#endregion

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
		private WorkspaceViewModel workspace;
	}
}
