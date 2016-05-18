using System;
using System.Linq;
using System.Reactive.Linq;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ReactiveUI;
using ShaderEditorApp.Model.Editor;

namespace ShaderEditorApp.View
{
	internal class OverloadProvider : ReactiveObject, IOverloadProvider
	{
		private SignatureHelp _signatureHelp;

		public OverloadProvider(SignatureHelp signatureHelp)
		{
			_signatureHelp = signatureHelp;

			this.WhenAnyValue(x => x.SelectedIndex)
				.Select(index => _signatureHelp.Overloads.ElementAt(index).Label)
				.ToProperty(this, x => x.CurrentHeader, out _currentHeader);

			this.WhenAnyValue(x => x.SelectedIndex)
				.Select(index => _signatureHelp.Overloads.ElementAt(index).Documentation)
				.ToProperty(this, x => x.CurrentContent, out _currentContent);

			this.WhenAnyValue(x => x.SelectedIndex)
				.Select(index => $"{index + 1} of {Count}")
				.ToProperty(this, x => x.CurrentIndexText, out _currentIndexText);

			SelectedIndex = _signatureHelp.BestOverload;
		}

		public int Count => _signatureHelp.Overloads.Count();

		private ObservableAsPropertyHelper<object> _currentHeader;
		public object CurrentHeader => _currentHeader.Value;

		private ObservableAsPropertyHelper<object> _currentContent;
		public object CurrentContent => _currentContent.Value;

		private ObservableAsPropertyHelper<string> _currentIndexText;
		public string CurrentIndexText => _currentIndexText.Value;

		private int _selectedIndex;
		public int SelectedIndex
		{
			get { return _selectedIndex; }
			set { this.RaiseAndSetIfChanged(ref _selectedIndex, value); }
		}
	}
}