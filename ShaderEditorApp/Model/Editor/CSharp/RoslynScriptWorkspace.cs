using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Roslyn Workspace for script files (something Roslyn itself currently lacks).
	// AdhocWorkspace comes close, but can't handle document change notifications.
	class RoslynScriptWorkspace : Microsoft.CodeAnalysis.Workspace
	{
		// TODO: What are HostServices? Do we need them?
		public RoslynScriptWorkspace()
			: base(MefHostServices.DefaultHost, WorkspaceKind.Host)
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
	}
}
