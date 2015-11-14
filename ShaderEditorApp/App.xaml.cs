using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ShaderEditorApp.Interfaces;
using ShaderEditorApp.Services;
using Splat;

namespace ShaderEditorApp
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			// Register services.
			Locator.CurrentMutable.RegisterLazySingleton(() => new WpfUserPrompt(), typeof(IUserPrompt));
			Locator.CurrentMutable.RegisterConstant(new WpfIsForegroundService(), typeof(IIsForegroundService));
		}
	}
}
