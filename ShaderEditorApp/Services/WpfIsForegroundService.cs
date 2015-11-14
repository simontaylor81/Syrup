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
			// Convert activated and deactivated events to observables.
			var activated = Observable.FromEventPattern(h => Application.Current.Activated += h, h => Application.Current.Activated -= h);
			var deactivated = Observable.FromEventPattern(h => Application.Current.Deactivated += h, h => Application.Current.Deactivated -= h);

			_isForeground = Observable.Merge(
				activated.Select(_ => true),
				deactivated.Select(_ => false))
				.ToProperty(this, x => x.IsAppForeground, false);

			// TEMP
			//this.WhenAnyValue(x => x.IsAppForeground).Subscribe(val => System.Diagnostics.Debug.WriteLine($"IsAppForeground = {val}"));
		}
	}
}
