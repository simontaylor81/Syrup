using System;
using System.Collections.Generic;
using System.Linq;
using SRPCommon.UserProperties;

namespace ShaderEditorApp.ViewModel.Properties
{
	static class PropertyViewModelFactory
	{
		private static Lazy<IEnumerable<IPropertyViewModelFactory>> _factories = new Lazy<IEnumerable<IPropertyViewModelFactory>>(FindFactories);

		// Create a new view-model object for a user property.
		public static PropertyViewModel CreateViewModel(IUserProperty property)
		{
			// Find the first factory that accepts the property.
			var factory = _factories.Value.FirstOrDefault(x => x.SupportsProperty(property));
			if (factory != null)
			{
				return factory.CreateInstance(property);
			}

			throw new ArgumentException("Unsupported property type");
		}

		private static IEnumerable<IPropertyViewModelFactory> FindFactories()
		{
			// Find all types that implement the factory interface in this assembly.
			return typeof(PropertyViewModelFactory).Assembly.GetTypes()
				.Where(type => !type.IsAbstract && type.GetInterfaces().Any(i => i == typeof(IPropertyViewModelFactory)))
				.Select(type => (IPropertyViewModelFactory)Activator.CreateInstance(type))
				.OrderBy(factory => factory.Priority)
				.ToList();
		}
	}

	// Interface for classes that create property view models.
	interface IPropertyViewModelFactory
	{
		bool SupportsProperty(IUserProperty property);
		PropertyViewModel CreateInstance(IUserProperty property);

		// Priority to allow selection order to be controlled. Lower numbers run first.
		int Priority { get; }
	}
}
