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
	public class RelayCommand : ICommand
	{
		#region Fields

		readonly Action<object> _execute;
		readonly Predicate<object> _canExecute;

		#endregion // Fields

		#region Constructors

		public RelayCommand(Action<object> execute)
			: this(execute, null)
		{
		}

		public RelayCommand(Action<object> execute, Predicate<object> canExecute)
		{
			if (execute == null)
				throw new ArgumentNullException(nameof(execute));

			_execute = execute;
			_canExecute = canExecute;
		}
		#endregion // Constructors

		#region ICommand Members

		[DebuggerStepThrough]
		public bool CanExecute(object parameter)
		{
			return _canExecute == null ? true : _canExecute(parameter);
		}

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameter)
		{
			_execute(parameter);
		}

		#endregion // ICommand Members

		public static RelayCommand LazyInit(ref RelayCommand cmdVar, Action<object> execute, Predicate<object> canExecute = null)
		{
			if (cmdVar == null)
				cmdVar = new RelayCommand(execute, canExecute);

			return cmdVar;
		}
	}

	public class NamedCommand : ICommand
	{
		public NamedCommand(string name, ICommand innerCommand)
		{
			Name = name;
			innerCmd = innerCommand;
		}

		public string Name { get; private set; }
		private ICommand innerCmd;

		// ICommand interface. Everything just passes through to the inner command.

		[DebuggerStepThrough]
		public bool CanExecute(object parameter)
		{
			return innerCmd.CanExecute(parameter);
		}

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
