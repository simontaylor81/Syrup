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

		// Inverse of IsExpanded for easier binding.
		private ObservableAsPropertyHelper<bool> _isCollapsed;
		public bool IsCollapsed => _isCollapsed.Value;

		public virtual bool CanExpand => false;

		protected PropertyViewModel(IUserProperty property)
		{
			DisplayName = property.Name;
			IsReadOnly = property.IsReadOnly;

			this.WhenAnyValue(x => x.IsExpanded, expanded => !expanded)
				.ToProperty(this, x => x.IsCollapsed, out _isCollapsed, initialValue: true);
		}
	}

	// View model base class for properties made up of other properties.
	public abstract class CompositePropertyViewModel : PropertyViewModel
	{
		public override bool CanExpand => true;

		// Immutable
		public abstract IEnumerable<PropertyViewModel> SubProperties { get; }

		public CompositePropertyViewModel(IUserProperty property)
			: base(property)
		{
		}
	}
}
