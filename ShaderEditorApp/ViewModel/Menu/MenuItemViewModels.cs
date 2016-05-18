﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ShaderEditorApp.Interfaces;
using ShaderEditorApp.Model;
using SRPCommon.Util;

namespace ShaderEditorApp.ViewModel.Menu
{
	// Base class for items on the menu.
	// Most things do nothing, but should be defined anyway to avoid spurious binding error messages in the log.
	public abstract class MenuItemViewModel : ReactiveObject
	{
		public virtual string Header => null;
		public virtual string Shortcut => null;
		public virtual bool IsEnabled { get; set; } = true;
		public virtual ICommand Command => null;
		public virtual object CommandParameter => null;
		public virtual IEnumerable<object> Items => null;
		public virtual bool IsCheckable => false;
		public virtual bool IsChecked
		{
			get { return false; }
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
			set { }
#pragma warning restore RECS0029
		}
	}

	// Class reprsenting a separator in the menu.
	public class SeparatorViewModel
	{
		public static SeparatorViewModel Instance { get; } = new SeparatorViewModel();
	}

	// A simple static menu item, potentially with sub-items.
	class StaticMenuItem : MenuItemViewModel
	{
		private IEnumerable<object> _items;
		public override IEnumerable<object> Items => _items;

		private string _header;
		public override string Header => _header;

		public StaticMenuItem(string header)
		{
			_header = header;
		}

		// Helpers for creating one with a list of sub-items.
		public static StaticMenuItem Create(string header, params object[] subItems)
			=> new StaticMenuItem(header) { _items = subItems };
	}

	// Menu item with an associated command view model.
	class CommandMenuItem : MenuItemViewModel
	{
		private readonly CommandViewModel _commandVM;

		public override string Header => _commandVM.MenuHeader;
		public override ICommand Command => _commandVM.Command;
		public override string Shortcut => _commandVM.KeyGestureString;

		public CommandMenuItem(CommandViewModel commandVM)
		{
			_commandVM = commandVM;
		}
	}

	// Menu item representing a raw command on its own without associated view model.
	class RawCommandMenuItem : MenuItemViewModel
	{
		private ICommand _command;
		public override ICommand Command => _command;

		private object _commandParameter;
		public override object CommandParameter => _commandParameter;

		private string _header;
		public override string Header => _header;

		public RawCommandMenuItem(string header, ICommand command, object parameter = null)
		{
			_header = header;
			_command = command;
			_commandParameter = parameter;
		}
	}

	// Menu item for a RoutedCommand, with menu text and shortcut string coming from the command.
	class RoutedCommandMenuItemViewModel : MenuItemViewModel
	{
		private readonly RoutedCommand _command;

		public override string Header => _command.Name;
		public override ICommand Command => _command;

		public override string Shortcut
		{
			get
			{
				var keyGestures = _command.InputGestures.OfType<KeyGesture>();
				return keyGestures.FirstOrDefault()?.DisplayString;
			}
		}

		public RoutedCommandMenuItemViewModel(RoutedCommand command)
		{
			_command = command;
		}
	}

	// Menu item that can be toggled on and off.
	class CheckableMenuItem : MenuItemViewModel
	{
		private readonly Func<bool> _get;
		private readonly Action<bool> _set;

		private string _header;
		public override string Header => _header;

		public override bool IsCheckable => true;

		// TODO: Support change notification.
		public override bool IsChecked
		{
			get { return _get(); }
			set { _set(value); }
		}

		public CheckableMenuItem(string header, Func<bool> get, Action<bool> set)
		{
			_header = header;
			_get = get;
			_set = set;
		}
	}

	// Menu item containing a list of recently opened files.
	class RecentFilesMenuItem : MenuItemViewModel
	{
		private ObservableAsPropertyHelper<IEnumerable<object>> _subitems;
		public override IEnumerable<object> Items => _subitems.Value;

		private string _header;
		public override string Header => _header;

		public RecentFilesMenuItem(string header, string noFilesText, IRecentFileList recentFiles, ICommand openCommand)
		{
			_header = header;

			// Sub menu to display when there are no recent files.
			var emptyMenu = new object[]
			{
				new StaticMenuItem(noFilesText) { IsEnabled = false }
			};

			// Build new list when the underlying file list changes.
			// Can't use CreateDerivedList as we want to display the "no items" item when it's empty.
			_subitems = recentFiles.Files.Changed
				.StartWithDefault()
				.Select(_ => recentFiles.Files.Any()
					? recentFiles.Files.Select((file, index) => (object)new RawCommandMenuItem(GetHeader(file, index), openCommand, file))
					: emptyMenu)
				.ToProperty(this, x => x.Items);
		}

		private string GetHeader(string path, int index)
		{
			// Indices should be one based in the UI.
			Trace.Assert(index >= 0);
			index++;

			// Mnemonic on the index for 1-9, on the 0 for 10, and nothing for >10.
			string prefix;
			if (index < 10)
			{
				prefix = $"_{index} ";
			}
			else if (index == 10)
			{
				prefix = "1_0 ";
			}
			else
			{
				prefix = $"{index} ";
			}

			// Escape any underscores in the path with double-underscores.
			return prefix + path.Replace("_", "__");
		}
	}
}
