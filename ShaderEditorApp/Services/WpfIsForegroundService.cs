using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using ShaderEditorApp.Interfaces;

namespace ShaderEditorApp.Services
{
	// Service class for determining if a WPF app is in the foreground or not.
	// Must be constructed at startup before the initial activation event.
	class WpfIsForegroundService : ReactiveObject, IIsForegroundService
	{
		private ObservableAsPropertyHelper<bool> _isForeground;
		public bool IsAppForeground => _isForeground.Value;

		public WpfIsForegroundService()
		{
			// Merge activated and deactivated into bool stream.
			_isForeground = Observable.Merge(
				Application.Current.Events().Activated.Select(_ => true),
				Application.Current.Events().Deactivated.Select(_ => false))
				.ToProperty(this, x => x.IsAppForeground, false);
		}
	}
}
