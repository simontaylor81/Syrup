using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ShaderEditorApp.MVVMUtil
{
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
