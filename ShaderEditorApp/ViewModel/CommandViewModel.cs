using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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

		// Keyboard gesture.
		public KeyGesture KeyGesture { get; }

		// Textual representation of the key gesture.
		public string KeyGestureString => KeyGesture?.GetDisplayStringForCulture(CultureInfo.CurrentCulture);

		public CommandViewModel(string name, IReactiveCommand command, string menuHeader = null, KeyGesture keyGesture = null)
		{
			Name = name;
			Command = command;
			MenuHeader = menuHeader ?? name;
			KeyGesture = keyGesture;
		}
	}

	// Simple helper that probably should have been in ReactiveUI.
	public static class CommandUtil
	{
		public static ReactiveCommand<object> Create(Action<object> execute, IObservable<bool> canExecute = null)
		{
			var command = ReactiveCommand.Create(canExecute);
			command.Subscribe(execute);
			return command;
		}
	}
}
