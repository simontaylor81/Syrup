using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ShaderEditorApp.ViewModel
{
	// The view-model for a command, that can be put on menus, bound to a key, etc.
	// Immutable, so no need for property notifications or any Rx nonsense.
	public class CommandViewModel
	{
		// Name of the command.
		public string Name { get; }

		// String to display in a menu for the command.
		public string MenuHeader { get; }

		// The command itself.
		public IReactiveCommand Command { get; }

		public CommandViewModel(string name, IReactiveCommand command)
		{
			Name = name;
			MenuHeader = name;
			Command = command;
		}

		public CommandViewModel(string name, string menuHeader, IReactiveCommand command)
		{
			Name = name;
			MenuHeader = menuHeader;
			Command = command;
		}
	}
}
