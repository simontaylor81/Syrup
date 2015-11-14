using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderEditorApp.Interfaces
{
	// Interface for a service to determine if the application is in the foreground or not.
	public interface IIsForegroundService : INotifyPropertyChanged
	{
		bool IsAppForeground { get; }
	}
}
