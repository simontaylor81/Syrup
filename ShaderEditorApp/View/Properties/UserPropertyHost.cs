using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ShaderEditorApp.ViewModel.Properties;

namespace ShaderEditorApp.View.Properties
{
	internal interface IPropertyViewFactory
	{
		bool SupportsProperty(PropertyViewModel property);
		FrameworkElement CreateView(PropertyViewModel property);

		// Priority to allow selection order to be controlled. Lower numbers run first.
		int Priority { get; }
	}

	// ContentControl for hosting the appropriate view for a user property.
	class UserPropertyHost : ContentControl
	{
		private static Lazy<IEnumerable<IPropertyViewFactory>> _factories = new Lazy<IEnumerable<IPropertyViewFactory>>(FindFactories);

		public UserPropertyHost()
		{
			DataContextChanged += OnDataContextChanged;
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var property = DataContext as PropertyViewModel;
			if (property == null)
			{
				throw new Exception("UserPropertyHost.DataContext must be an instance of PropertyViewMode. Got " + DataContext.GetType().Name);
			}

			// Find the first factory that accepts the property.
			var factory = _factories.Value.FirstOrDefault(x => x.SupportsProperty(property));
			if (factory != null)
			{
				Content = factory.CreateView(property);
			}
			else
			{
				throw new ArgumentException($"Could not find view for property '{property.DisplayName}' type = '{property.GetType().Name}'");
			}
		}

		private static IEnumerable<IPropertyViewFactory> FindFactories()
		{
			// Find all types that implement the factory interface in this assembly.
			return typeof(UserPropertyHost).Assembly.GetTypes()
				.Where(type => !type.IsAbstract && type.GetInterfaces().Any(i => i == typeof(IPropertyViewFactory)))
				.Select(type => (IPropertyViewFactory)Activator.CreateInstance(type))
				.OrderBy(factory => factory.Priority)
				.ToList();
		}
	}
}
