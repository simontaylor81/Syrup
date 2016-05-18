﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ShaderEditorApp.Model.Editor
{
	// Services for a single open document.
	public interface IDocumentServices : IDisposable
	{
		Task<CodeLocation> FindDefinitionAsync(int position);
		Task<string> GetCodeTipAsync(int position, CancellationToken cancellationToken);
		Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken cancellationToken);
		Task<IEnumerable<CompletionItem>> GetCompletions(int position, char? triggerChar, CancellationToken cancellationToken);
		Task<SignatureHelp> GetSignatureHelp(int position, CancellationToken cancellationToken);

		bool IsSignatureHelpTriggerChar(char c);
		bool IsSignatureHelpEndChar(char c);
	}
}