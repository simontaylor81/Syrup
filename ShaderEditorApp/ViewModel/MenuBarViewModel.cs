using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.ViewModel;
using ShaderEditorApp.MVVMUtil;
using System.Windows.Input;
using ShaderEditorApp.Model;
using ReactiveUI;
using System.Reactive.Linq;
using SRPCommon.Util;

namespace ShaderEditorApp.ViewModel
{
	// View model for the main menu bar of the application.
	public class MenuBarViewModel : ReactiveUI.ReactiveObject
	{
		// Items that make up the menu. Untyped to allow menu items and separators.
		public IEnumerable<object> Items { get; }

		public MenuBarViewModel(WorkspaceViewModel workspace)
		{
			Items = new object[]
			{
				// File menu
				StaticMenuItemViewModel.Create("File",
					// New submenu
					StaticMenuItemViewModel.Create("New",
						new CommandViewModelMenuItemViewModel(workspace.NewProject) { Shortcut = "Ctrl+Shift+N" },
						new CommandViewModelMenuItemViewModel(workspace.NewDocument) { Shortcut = "Ctrl+N" }),

					// Open submenu
					StaticMenuItemViewModel.Create("Open",
						new CommandViewModelMenuItemViewModel(workspace.OpenProject) { Shortcut = "Ctrl+Shift+O" },
						new CommandViewModelMenuItemViewModel(workspace.OpenDocument) { Shortcut = "Ctrl+O" }),

					new NamedCommandMenuItemViewModel(workspace.SaveActiveDocumentCmd) { Shortcut = "Ctrl+S" },
					new NamedCommandMenuItemViewModel(workspace.SaveAllCmd) { Shortcut = "Ctrl+Shift+S" },
					new NamedCommandMenuItemViewModel(workspace.SaveActiveDocumentAsCmd),
					new NamedCommandMenuItemViewModel(workspace.CloseActiveDocumentCmd) { Shortcut = "Ctrl+F4" },

					SeparatorViewModel.Instance,

					// Recently opened projects and files sub-menus.
					new RecentFilesMenuItemViewModel(
						"Recent Projects", "No Projects", workspace.Workspace.UserSettings.RecentProjects, workspace.OpenProjectFile),
					new RecentFilesMenuItemViewModel(
						"Recent Files", "No Files", workspace.Workspace.UserSettings.RecentFiles, workspace.OpenDocumentFile),

					SeparatorViewModel.Instance,

					new CommandMenuItemViewModel(workspace.ExitCmd) { Header = "Exit", Shortcut = "Alt+F4" }
				),

				// Edit menu
				StaticMenuItemViewModel.Create("Edit",
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Undo),
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Redo),
					SeparatorViewModel.Instance,
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Cut),
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Copy),
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Paste)
				),

				// View menu
				StaticMenuItemViewModel.Create("View",
					new CheckableMenuItemViewModel(() => workspace.RealTimeMode, x => workspace.RealTimeMode = x) { Header = "Real-Time Mode" }
				),

				// Run menu
				StaticMenuItemViewModel.Create("Run",
					new NamedCommandMenuItemViewModel(workspace.RunActiveScriptCmd) { Shortcut = "F5" }
				)
			};
		}
	}

	// Base class for items on the menu.
	// Most things do nothing, but should be defined anyway to avoid spurious binding error messages in the log.
	public abstract class MenuItemViewModel : ReactiveObject
	{
		public virtual string Header { get; set; }
		public virtual string Shortcut { get; set; }
		public virtual bool IsEnabled { get; set; } = true;
		public virtual ICommand Command => null;
		public virtual object CommandParameter => null;
		public virtual IEnumerable<object> Items => null;
		public virtual bool IsCheckable => false;
		public virtual bool IsChecked
		{
			get { return false; }
			set { }
		}
	}

	// Class reprsenting a separator in the menu.
	public class SeparatorViewModel
	{
		public static SeparatorViewModel Instance { get; } = new SeparatorViewModel();
	}

	// A simple static menu item, potentially with sub-items.
	class StaticMenuItemViewModel : MenuItemViewModel
	{
		private IEnumerable<object> _items;
		public override IEnumerable<object> Items => _items;

		// Helpers for creating one with a list of sub-items.
		public static StaticMenuItemViewModel Create(string header, params object[] subItems)
			=> new StaticMenuItemViewModel
				{
					Header = header,
					_items = subItems
				};
	}

	// Menu item representing a simple command.
	class CommandMenuItemViewModel : MenuItemViewModel
	{
		public override ICommand Command { get; }
		public override object CommandParameter { get; }

		public CommandMenuItemViewModel(ICommand command, object parameter = null)
		{
			Command = command;
			CommandParameter = parameter;
		}
	}

	// Menu item representing a named command, where the menu text comes from the name.
	class NamedCommandMenuItemViewModel : MenuItemViewModel
	{
		private readonly INamedCommand _command;

		public override string Header => _command.Name;
		public override ICommand Command => _command;

		public NamedCommandMenuItemViewModel(INamedCommand command)
		{
			_command = command;
		}
	}

	// Menu item with an associated command view model.
	// TODO: Rename.
	class CommandViewModelMenuItemViewModel : MenuItemViewModel
	{
		private readonly CommandViewModel _commandVM;

		public override string Header => _commandVM.MenuHeader;
		public override ICommand Command => _commandVM.Command;

		public CommandViewModelMenuItemViewModel(CommandViewModel commandVM)
		{
			_commandVM = commandVM;
		}
	}

	// Menu item for a RoutedCommand, with menu text and shortcut string coming from the command.
	class RoutedCommandMenuItemViewModel : MenuItemViewModel
	{
		private readonly RoutedCommand _command;

		public override string Header => _command.Name;
		public override ICommand Command => _command;

		public override string Shortcut
		{
			get
			{
				var keyGestures = _command.InputGestures.OfType<KeyGesture>();
				return keyGestures.FirstOrDefault()?.DisplayString;
			}
		}

		public RoutedCommandMenuItemViewModel(RoutedCommand command)
		{
			_command = command;
		}
	}

	// Menu item that can be toggled on and off.
	class CheckableMenuItemViewModel : MenuItemViewModel
	{
		private readonly Func<bool> _get;
		private readonly Action<bool> _set;

		public override bool IsCheckable => true;

		// TODO: Support change notification.
		public override bool IsChecked
		{
			get { return _get(); }
			set { _set(value); }
		}

		public CheckableMenuItemViewModel(Func<bool> get, Action<bool> set)
		{
			_get = get;
			_set = set;
		}
	}

	// Menu item containing a list of recently opened files.
	class RecentFilesMenuItemViewModel : MenuItemViewModel
	{
		private ObservableAsPropertyHelper<IEnumerable<object>> _subitems;
		public override IEnumerable<object> Items => _subitems.Value;

		public RecentFilesMenuItemViewModel(string header, string noFilesText, RecentFileList recentFiles, ICommand openCommand)
		{
			Header = header;

			// Sub menu to display when there are no recent files.
			var emptyMenu = new object[]
			{
				new StaticMenuItemViewModel() { Header = noFilesText, IsEnabled = false }
			};

			// Build new list when the underlying file list changes.
			// Can't use CreateDerivedList as we want to display the "no items" item when it's empty.
			_subitems = recentFiles.Files.Changed
				.StartWithDefault()
				.Select(_ => recentFiles.Files.Any()
					? recentFiles.Files.Select(file => (object)new CommandMenuItemViewModel(openCommand, file) { Header = file })
					: emptyMenu)
				.ToProperty(this, x => x.Items);
		}
	}
}
