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

			var commands = new List<NamedCommand>
				{
					OpenCmd,
					RemoveCmd,
				};

			if (item.IsScript)
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

				if (item.IsScript)
				{
					// Add a 'run on start-up' property for script items.
					var runOnStartupProp = new SimpleScalarProperty<bool>("Run at Startup", item.RunAtStartup, b => item.RunAtStartup = b);

					result = result.Concat(new[] { runOnStartupProp });
				}

				return result;
			}
		}

		public string ItemTypeString
		{
			get
			{
				switch (item.Extension)
				{
					case ".py":
						return "Script";
					case ".hlsl":
					case ".fx":
						return "Shader";
					default:
						return "Other";
				}
			}
		}

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

		// Command to execute the script file the project item represents.
		private NamedCommand runCmd;
		public NamedCommand RunCmd
		{
			get
			{
				return NamedCommand.LazyInit(ref runCmd, "Run",
					(param) => workspace.RunScriptFile(item.AbsolutePath),
					(param) => item.IsScript);
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
