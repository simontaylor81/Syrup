using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.ViewModel;
using ShaderEditorApp.MVVMUtil;
using System.Windows.Input;

namespace ShaderEditorApp.ViewModel
{
	// View model for the main menu bar of the application.
	public class MenuBarViewModel : ReactiveUI.ReactiveObject
	{
		public IEnumerable<MenuItemViewModel> Items { get; }

		public MenuBarViewModel(WorkspaceViewModel workspace)
		{
			Items = new[]
			{
				// File menu
				StaticMenuItemViewModel.Create("File",
					// New submenu
					StaticMenuItemViewModel.Create("New",
						new CommandMenuItemViewModel(workspace.NewProjectCmd) { Header ="Project", Shortcut = "Ctrl+Shift+N" },
						new CommandMenuItemViewModel(workspace.NewDocumentCmd) { Header = "Document", Shortcut = "Ctrl+N" }),

					// Open submenu
					StaticMenuItemViewModel.Create("Open",
						new CommandMenuItemViewModel(workspace.OpenProjectCmd) { Header = "Project", Shortcut = "Ctrl+Shift+O" },
						new CommandMenuItemViewModel(workspace.OpenDocumentCmd) { Header = "Document", Shortcut = "Ctrl+O" }),

					new NamedCommandMenuItemViewModel(workspace.SaveActiveDocumentCmd) { Shortcut = "Ctrl+S" },
					new NamedCommandMenuItemViewModel(workspace.SaveAllCmd) { Shortcut = "Ctrl+Shift+S" },
					new NamedCommandMenuItemViewModel(workspace.SaveActiveDocumentAsCmd),
					new NamedCommandMenuItemViewModel(workspace.CloseActiveDocumentCmd) { Shortcut = "Ctrl+F4" },
					new StaticMenuItemViewModel { Header = "Exit" }		// TODO!
				),

				// Edit menu
				StaticMenuItemViewModel.Create("Edit",
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Undo),
					new RoutedCommandMenuItemViewModel(ApplicationCommands.Redo),
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
	public abstract class MenuItemViewModel
	{
		public virtual string Header { get; set; }
		public virtual string Shortcut { get; set; }
		public virtual ICommand Command => null;
		public virtual IEnumerable<MenuItemViewModel> Items => null;
		public virtual bool IsCheckable => false;
		public virtual bool IsChecked
		{
			get { return false; }
			set { }
		}
	}

	// A simple static menu item, potentially with sub-items.
	class StaticMenuItemViewModel : MenuItemViewModel
	{
		private IEnumerable<MenuItemViewModel> _items;
		public override IEnumerable<MenuItemViewModel> Items => _items;

		// Helpers for creating one with a list of sub-items.
		public static StaticMenuItemViewModel Create(string header, params MenuItemViewModel[] subItems)
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

		public CommandMenuItemViewModel(ICommand command)
		{
			Command = command;
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
}
