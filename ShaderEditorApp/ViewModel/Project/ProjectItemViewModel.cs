using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.ViewModel;
using ShaderEditorApp.Workspace;
using SRPCommon.UserProperties;
using System.Windows.Input;
using ShaderEditorApp.Projects;
using System.Reactive.Disposables;

namespace ShaderEditorApp.ViewModel.Projects
{
	public class ProjectItemViewModel : ViewModelBase, IHierarchicalBrowserNodeViewModel
	{
		public ProjectItemViewModel(ProjectItem item, Project project, WorkspaceViewModel workspace)
		{
			this.item = item;
			DisplayName = item.Name;
			this.project = project;
			this.workspace = workspace;

			// Notifty that the default property may have changed when the default scene.
			disposables.Add(project.DefaultSceneChanged.Subscribe(_ => OnPropertyChanged("IsDefault")));

			// Scenes are set as current, everything else is opened.
			_defaultCmd = (item.Type != ProjectItemType.Scene) ? OpenCmd : SetCurrentSceneCmd;

			var commands = new List<NamedCommand>
				{
					_defaultCmd,
					RemoveCmd,
				};

			if (item.Type == ProjectItemType.Script)
			{
				commands.Add(RunCmd);
			}
			else if (item.Type == ProjectItemType.Scene)
			{
				commands.Add(SetDefaultCmd);
			}

			Commands = commands;

			CreateUserProperties();
		}

		protected override void OnDispose()
		{
			disposables.Dispose();
			base.OnDispose();
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
		public bool IsDefault { get { return item.IsDefault; } }

		#endregion

		#region Commands

		// Command to open the project item into the workspace.
		private NamedCommand openCmd;
		public NamedCommand OpenCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref openCmd, "Open",
					(param) => workspace.OpenDocument(item.AbsolutePath, false));
			}
		}

		// Command to open a scene and make it the current scene for the workspace.
		private NamedCommand setCurrentSceneCmd;
		public NamedCommand SetCurrentSceneCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref setCurrentSceneCmd, "Set as Current",
					(param) => workspace.SetCurrentScene(item.AbsolutePath));
			}
		}

		// Command to execute the script file the project item represents.
		private NamedCommand runCmd;
		public NamedCommand RunCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref runCmd, "Run",
					(param) => workspace.RunScriptFile(item.AbsolutePath),
					(param) => item.Type == ProjectItemType.Script);
			}
		}

		// Command to remove the item from the project.
		private NamedCommand removeCmd;
		public NamedCommand RemoveCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref removeCmd, "Remove",
					(param) => RemoveFromProject());
			}
		}

		// Set the project item as the default of its type (scene's only, currently).
		private NamedCommand setDefaultCmd;
		public NamedCommand SetDefaultCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref setDefaultCmd, "Set as default",
					(param) => item.SetAsDefault(),
					(param) => item.Type == ProjectItemType.Scene);
			}
		}

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
		private CompositeDisposable disposables = new CompositeDisposable();
	}
}
