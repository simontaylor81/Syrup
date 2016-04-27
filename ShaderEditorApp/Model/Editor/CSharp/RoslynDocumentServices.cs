
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Roslyn-based editor services for individual documents.
	internal class RoslynDocumentServices : IDocumentServices
	{
		private readonly DocumentId _documentId;
		private readonly RoslynScriptWorkspace _roslynWorkspace;
		private readonly Action _onDispose;

		private readonly CompletionServiceWrapper _completionService = new CompletionServiceWrapper();

		public RoslynDocumentServices(
			RoslynScriptWorkspace roslynWorkspace,
			DocumentId documentId,
			Action onDispose)
		{
			_roslynWorkspace = roslynWorkspace;
			_documentId = documentId;
			_onDispose = onDispose;
		}

		public void Dispose()
		{
			_onDispose?.Invoke();
		}

		public async Task<TextSpan?> FindDefinitionAsync(int position)
		{
			var symbol = await GetSymbol(position);
			if (symbol != null)
			{
				// TODO: Handle symbols in other files.
				// TODO: Handle multiple locations?
				var location = symbol.Locations.First();
				return location.SourceSpan;
			}

			return null;
		}

		public async Task<string> GetCodeTipAsync(int position, CancellationToken cancellationToken)
		{
			var symbol = await GetSymbol(position);
			if (symbol != null)
			{
				return CodeTipFormatter.FormatCodeTip(symbol);
			}

			return null;
		}

		public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken cancellationToken)
		{
			var document = _roslynWorkspace.CurrentSolution.GetDocument(_documentId);
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			return semanticModel.GetDiagnostics();
		}

		public async Task<IEnumerable<CompletionItem>> GetCompletions(int position, char? triggerChar, CancellationToken cancellationToken)
		{
			var document = _roslynWorkspace.CurrentSolution.GetDocument(_documentId);

			// If we have a trigger character, check if it should trigger stuff.
			if (triggerChar == null ||
				await _completionService.IsCompletionTriggerCharacterAsync(document, position - 1, cancellationToken))
			{
				return await _completionService.GetCompletionListAsync(document, position, triggerChar, cancellationToken);
			}
			return Enumerable.Empty<CompletionItem>();
		}

		private Task<ISymbol> GetSymbol(int position)
		{
			var document = _roslynWorkspace.CurrentSolution.GetDocument(_documentId);
			return SymbolFinder.FindSymbolAtPositionAsync(document, position);
		}
	}
}
