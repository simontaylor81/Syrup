using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Services for a single open document.
	public interface IDocumentServices : IDisposable
	{
		Task<TextSpan?> FindDefinitionAsync(int position);
		Task<string> GetCodeTipAsync(int position, CancellationToken cancellationToken);
		Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken cancellationToken);
		Task<IEnumerable<CompletionItem>> GetCompletions(int position, char? triggerChar, CancellationToken cancellationToken);
		Task<SignatureHelp> GetSignatureHelp(int position, CancellationToken cancellationToken);
	}
}
