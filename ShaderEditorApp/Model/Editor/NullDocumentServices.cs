using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ShaderEditorApp.Model.Editor
{
	// IDocumentServices implementation that does nothing, for unsupported file types.
	internal class NullDocumentServices : IDocumentServices
	{
		public void Dispose() { }

		public Task<CodeLocation> FindDefinitionAsync(int position) => Task.FromResult<CodeLocation>(null);

		public Task<string> GetCodeTipAsync(int position, CancellationToken cancellationToken) => Task.FromResult<string>(null);

		public Task<IEnumerable<CompletionItem>> GetCompletions(int position, char? triggerChar, CancellationToken cancellationToken)
			=> Task.FromResult(Enumerable.Empty<CompletionItem>());

		public Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken cancellationToken)
			=> Task.FromResult(ImmutableArray<Diagnostic>.Empty);

		public Task<SignatureHelp> GetSignatureHelp(int position, CancellationToken cancellationToken) => Task.FromResult<SignatureHelp>(null);

		public bool IsSignatureHelpEndChar(char c) => false;

		public bool IsSignatureHelpTriggerChar(char c) => false;
	}
}
