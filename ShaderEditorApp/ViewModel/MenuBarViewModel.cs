using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShaderEditorApp.Workspace;
using ShaderEditorApp.MVVMUtil;
using System.Windows.Input;
using ReactiveUI;

namespace ShaderEditorApp.ViewModel
{
	// View model for the main menu bar of the application.
	public class MenuBarViewModel : ReactiveUI.ReactiveObject
	{
		public IEnumerable<IMenuItemViewModel> Items { get; }

		public MenuBarViewModel(WorkspaceViewModel workspace)
		{
			var fileItems = new IMenuItemViewModel[]
			{
				new StaticMenuItemViewModel
				{
					Header ="New",
					Items = new[]
					{
						new CommandMenuItemViewModel(workspace.NewProjectCmd) { Header ="Project", Shortcut = "Ctrl+Shift+N" },
						new CommandMenuItemViewModel(workspace.NewDocumentCmd) { Header = "Document", Shortcut = "Ctrl+N" },
					}
				},
				new StaticMenuItemViewModel
				{
					Header = "Open",
					Items = new[]
					{
						new CommandMenuItemViewModel(workspace.OpenProjectCmd) { Header = "Project", Shortcut = "Ctrl+Shift+O" },
						new CommandMenuItemViewModel(workspace.OpenDocumentCmd) { Header = "Document", Shortcut = "Ctrl+O" },
					}
				},
				new NamedCommandMenuItemViewModel(workspace.SaveActiveDocumentCmd) { Shortcut = "Ctrl+S" },
				new NamedCommandMenuItemViewModel(workspace.SaveAllCmd) { Shortcut = "Ctrl+Shift+S" },
				new NamedCommandMenuItemViewModel(workspace.SaveActiveDocumentAsCmd),
				new NamedCommandMenuItemViewModel(workspace.CloseActiveDocumentCmd) { Shortcut = "Ctrl+F4" },
				new StaticMenuItemViewModel { Header = "Exit" },	// TODO!
			};

			var editItems = new[]
			{
				new RoutedCommandMenuItemViewModel(ApplicationCommands.Undo),
				new RoutedCommandMenuItemViewModel(ApplicationCommands.Redo),
				new RoutedCommandMenuItemViewModel(ApplicationCommands.Cut),
				new RoutedCommandMenuItemViewModel(ApplicationCommands.Copy),
				new RoutedCommandMenuItemViewModel(ApplicationCommands.Paste),
			};

			var viewItems = new IMenuItemViewModel[]
			{
				new CheckableMenuItemViewModel(() => workspace.RealTimeMode, x => workspace.RealTimeMode = x) { Header = "Real-Time Mode" },
			};

			var runItems = new IMenuItemViewModel[]
			{
				new NamedCommandMenuItemViewModel(workspace.RunActiveScriptCmd) { Shortcut = "F5" },
			};

			Items = new[]
			{
				new StaticMenuItemViewModel { Header = "File", Items = fileItems },
				new StaticMenuItemViewModel { Header = "Edit", Items = editItems },
				new StaticMenuItemViewModel { Header = "View", Items = viewItems },
				new StaticMenuItemViewModel { Header = "Run", Items = runItems },
			};
		}
	}

	public interface IMenuItemViewModel
	{
		string Header { get; }
		string Shortcut { get; }
		ICommand Command { get; }
		IEnumerable<IMenuItemViewModel> Items { get; }
	}

	public class StaticMenuItemViewModel : IMenuItemViewModel
	{
		public string Header { get; set; }
		public string Shortcut { get; set; }
		public ICommand Command => null;

		public IEnumerable<IMenuItemViewModel> Items { get; set; }
	}

	class CommandMenuItemViewModel : IMenuItemViewModel
	{
		public string Header { get; set; }
		public ICommand Command { get; }
		public string Shortcut { get; set; }

		public IEnumerable<IMenuItemViewModel> Items => null;

		public CommandMenuItemViewModel(ICommand command)
		{
			Command = command;
		}
	}

	class NamedCommandMenuItemViewModel : IMenuItemViewModel
	{
		private readonly INamedCommand _command;

		public string Header => _command.Name;
		public ICommand Command => _command;
		public IEnumerable<IMenuItemViewModel> Items => null;

		public string Shortcut { get; set; }

		public NamedCommandMenuItemViewModel(INamedCommand command)
		{
			_command = command;
		}
	}

	class RoutedCommandMenuItemViewModel : IMenuItemViewModel
	{
		private readonly RoutedCommand _command;

		public string Header => _command.Name;
		public ICommand Command => _command;
		public IEnumerable<IMenuItemViewModel> Items => null;

		public string Shortcut
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

	class CheckableMenuItemViewModel : IMenuItemViewModel
	{
		private readonly Func<bool> _get;
		private readonly Action<bool> _set;

		public string Header { get; set; }
		public string Shortcut { get; set; }

		public bool IsCheckable => true;

		// TODO: Support change notification.
		public bool IsChecked
		{
			get { return _get(); }
			set { _set(value); }
		}

		public ICommand Command => null;
		public IEnumerable<IMenuItemViewModel> Items => null;

		public CheckableMenuItemViewModel(Func<bool> get, Action<bool> set)
		{
			_get = get;
			_set = set;
		}
	}
}
