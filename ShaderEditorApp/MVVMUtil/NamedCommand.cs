using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShaderEditorApp.MVVMUtil
{
	public interface INamedCommand : ICommand
	{
		string Name { get; }
	}

	public class NamedCommand : INamedCommand
	{
		public NamedCommand(string name, ICommand innerCommand)
		{
			Name = name;
			innerCmd = innerCommand;
		}

		public string Name { get; }
		private ICommand innerCmd;

		// ICommand interface. Everything just passes through to the inner command.

		[DebuggerStepThrough]
		public bool CanExecute(object parameter) => innerCmd.CanExecute(parameter);

		public event EventHandler CanExecuteChanged
		{
			add { innerCmd.CanExecuteChanged += value; }
			remove { innerCmd.CanExecuteChanged -= value; }
		}

		public void Execute(object parameter)
		{
			innerCmd.Execute(parameter);
		}

		public static NamedCommand LazyInit(ref NamedCommand cmdVar, string name, Action<object> execute, Predicate<object> canExecute = null)
		{
			if (cmdVar == null)
				cmdVar = new NamedCommand(name, new RelayCommand(execute, canExecute));

			return cmdVar;
		}

		public static NamedCommand CreateReactive(string name, Action<object> execute)
		{
			var reactiveCommand = ReactiveCommand.Create();
			reactiveCommand.Subscribe(execute);
			return new NamedCommand(name, reactiveCommand);
		}
	}
}
