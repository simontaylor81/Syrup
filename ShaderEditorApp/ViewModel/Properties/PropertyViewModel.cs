using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SRPCommon.UserProperties;
using ReactiveUI;

namespace ShaderEditorApp.ViewModel.Properties
{
	// Represents a property that can be displayed and edited in the properties window.
	// Properties have a type, a name and a value.
	public abstract class PropertyViewModel : ReactiveObject
	{
		public string DisplayName { get; }
		public bool IsReadOnly { get; }
		public virtual bool IsFullWidth => false;

		// Is the property currently expanded? Not used by all properties,
		// but having it in the base class anyway simplifies stuff.
		private bool _isExpanded = false;
		public bool IsExpanded
		{
			get { return _isExpanded; }
			set { this.RaiseAndSetIfChanged(ref _isExpanded, value); }
		}

		// Sub-properties for composite properties.
		// Immutable.
		public virtual IEnumerable<PropertyViewModel> SubProperties => Enumerable.Empty<PropertyViewModel>();

		// Can the property be expanded. Defaults to whether it has sub-properties,
		// but virtual in case property types want to handle it themselves.
		public virtual bool CanExpand => SubProperties.Any();

		protected PropertyViewModel(IUserProperty property)
		{
			DisplayName = property.Name;
			IsReadOnly = property.IsReadOnly;
		}
	}
}
