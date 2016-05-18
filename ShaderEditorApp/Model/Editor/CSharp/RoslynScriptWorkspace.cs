using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Roslyn Workspace for script files (something Roslyn itself currently lacks).
	// AdhocWorkspace comes close, but can't handle document change notifications.
	class RoslynScriptWorkspace : Microsoft.CodeAnalysis.Workspace
	{
		public RoslynScriptWorkspace(HostServices host)
			: base(host, WorkspaceKind.Host)
		{
		}

		public void OpenDocument(DocumentId documentId, SourceTextContainer container)
		{
			base.OpenDocument(documentId, true);
			OnDocumentOpened(documentId, container);
		}

		// Support all changes.
		public override bool CanApplyChange(ApplyChangesKind feature) => true;
		public override bool CanOpenDocuments => true;

		// Set a new Current Solution.
		public void SetSolution(Solution solution)
		{
			var oldSolution = CurrentSolution;
			var newSolution = base.SetCurrentSolution(solution);
			RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
		}
	}
}
