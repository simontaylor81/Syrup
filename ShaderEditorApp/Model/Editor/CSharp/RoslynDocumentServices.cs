
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

		private Document Document => _roslynWorkspace.CurrentSolution.GetDocument(_documentId);

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
			var semanticModel = await Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			return semanticModel.GetDiagnostics();
		}

		public async Task<IEnumerable<CompletionItem>> GetCompletions(int position, char? triggerChar, CancellationToken cancellationToken)
		{
			// If we have a trigger character, check if it should trigger stuff.
			if (triggerChar == null ||
				await _completionService.IsCompletionTriggerCharacterAsync(Document, position - 1, cancellationToken))
			{
				return await _completionService.GetCompletionListAsync(Document, position, triggerChar, cancellationToken);
			}
			return Enumerable.Empty<CompletionItem>();
		}

		public Task<SignatureHelp> GetSignatureHelp(int position, CancellationToken cancellationToken)
			=> SignatureHelpService.GetSignatureHelp(Document, position, cancellationToken);

		public bool IsSignatureHelpTriggerChar(char c) => SignatureHelpService.IsSignatureHelpTriggerChar(c);
		public bool IsSignatureHelpEndChar(char c) => SignatureHelpService.IsSignatureHelpEndChar(c);

		private Task<ISymbol> GetSymbol(int position) => SymbolFinder.FindSymbolAtPositionAsync(Document, position);
	}
}
