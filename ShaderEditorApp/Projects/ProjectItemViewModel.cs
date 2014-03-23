using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using ShaderEditorApp.MVVMUtil;
using ShaderEditorApp.ViewModel;
using ShaderEditorApp.Workspace;

namespace ShaderEditorApp.Projects
{
	public class ProjectItemViewModel : ProjectViewModelBase
	{
		public ProjectItemViewModel(ProjectItem item, Project project, WorkspaceViewModel workspace)
		{
			this.item = item;
			DisplayName = item.Name;
			this.project = project;
			this.workspace = workspace;

			// Scenes are set as current, everything else is opened.
			DefaultCmd = (item.Type != ProjectItemType.Scene) ? OpenCmd : SetCurrentSceneCmd;

			var commands = new List<NamedCommand>
				{
					DefaultCmd,
					RemoveCmd,
				};

			if (item.Type == ProjectItemType.Script)
			{
				commands.Add(RunCmd);
			}

			Commands = commands;
		}

		// Properties to display for the item.
		public override IEnumerable<PropertyViewModel> ItemProperties
		{
			get
			{
				// Add some read-only properties.
				var filenameProp = new SimpleScalarProperty<string>("Filename", DisplayName, true);
				var pathProp = new SimpleScalarProperty<string>("Full path", item.AbsolutePath, true);
				var typeProp = new SimpleScalarProperty<string>("Type", ItemTypeString, true);

				IEnumerable<PropertyViewModel> result = new[] { filenameProp, pathProp, typeProp };

				if (item.Type == ProjectItemType.Script)
				{
					// Add a 'run on start-up' property for script items.
					var runOnStartupProp = new SimpleScalarProperty<bool>("Run at Startup", item.RunAtStartup, b => item.RunAtStartup = b);

					result = result.Concat(new[] { runOnStartupProp });
				}

				return result;
			}
		}

		// Get type of item as a string.
		public string ItemTypeString { get { return Enum.GetName(typeof(ProjectItemType), item.Type); } }

		#region Commands

		// 'Default' command -- i.e. the one to execute when double clicking on the item.
		public NamedCommand DefaultCmd { get; private set; }

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
