﻿using System;
using System.Collections.Generic;
using System.Windows.Input;
using ShaderEditorApp.Interfaces;
using ShaderEditorApp.ViewModel.Workspace;
using Splat;

namespace ShaderEditorApp.ViewModel.Menu
{
	// View model for the main menu bar of the application.
	public class MenuBarViewModel : ReactiveUI.ReactiveObject
	{
		// Items that make up the menu. Untyped to allow menu items and separators.
		public IEnumerable<object> Items { get; }

		public MenuBarViewModel(WorkspaceViewModel workspace, IUserSettings userSettings = null)
		{
			userSettings = userSettings ?? Locator.Current.GetService<IUserSettings>();

			Items = new object[]
			{
				// File menu
				StaticMenuItem.Create("_File",
					// New submenu
					StaticMenuItem.Create("_New",
						new CommandMenuItem(workspace.NewProject),
						new CommandMenuItem(workspace.NewDocument)),

					// Open submenu
					StaticMenuItem.Create("_Open",
						new CommandMenuItem(workspace.OpenProject),
						new CommandMenuItem(workspace.OpenDocument)),

					new CommandMenuItem(workspace.SaveActiveDocument),
					new CommandMenuItem(workspace.SaveAll),
					new CommandMenuItem(workspace.SaveActiveDocumentAs),
					new CommandMenuItem(workspace.CloseActiveDocument),

					SeparatorViewModel.Instance,

					// Recently opened projects and files sub-menus.
					new RecentFilesMenuItem(
						"Recent Pro_jects", "No Projects", userSettings.RecentProjects, workspace.OpenProjectFile),
					new RecentFilesMenuItem(
						"Recent _Files", "No Files", userSettings.RecentFiles, workspace.OpenDocumentFile),

					SeparatorViewModel.Instance,

					new CommandMenuItem(workspace.ExitCommand)
				),

				// Edit menu
				StaticMenuItem.Create("_Edit",
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Undo),
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Redo),
					SeparatorViewModel.Instance,
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Cut),
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Copy),
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Paste),
					SeparatorViewModel.Instance,
					new CommandMenuItem(workspace.GoToDefinition)
				),

				// View menu
				StaticMenuItem.Create("_View",
					new CheckableMenuItem("Real-Time Mode", () => workspace.RealTimeMode, x => workspace.RealTimeMode = x)
				),

				// Run menu
				StaticMenuItem.Create("_Run",
					new CommandMenuItem(workspace.RunActiveScript)
				)
			};
		}
	}
}
