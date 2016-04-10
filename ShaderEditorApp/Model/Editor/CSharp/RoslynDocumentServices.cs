using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using SRPCommon.Scripting;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Wrapper for all sorts of Roslyn stuff.
	// Currently just one-per-document for simplicity.
	// We'll probably want to have multiple documents in a single workspace
	// eventually so we can get e.g. go to definition across files (without saving).
	public class RoslynDocumentServices
	{
		private readonly DocumentId _documentId;
		private readonly ProjectId _projectId;
		private readonly RoslynScriptWorkspace _roslynWorkspace;

		public RoslynDocumentServices(SourceTextContainer sourceTextContainer, string path)
		{
			// Create a Roslyn workspace.
			// Is adhoc workspace sufficient?
			_roslynWorkspace = new RoslynScriptWorkspace();

			// TODO: Source file resolver.
			var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

			var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script);

			var documentName = Path.GetFileName(path);
			var projectName = documentName + ".project";

			_projectId = ProjectId.CreateNewId(projectName);
			_documentId = DocumentId.CreateNewId(_projectId, documentName);

			var documentInfo = DocumentInfo.Create(
				_documentId,
				documentName,
				sourceCodeKind: SourceCodeKind.Script,
				loader: TextLoader.From(TextAndVersion.Create(sourceTextContainer.CurrentText, VersionStamp.Create())),
				filePath: path);

			var solution = _roslynWorkspace.CurrentSolution.AddProject(ProjectInfo.Create(
				_projectId,
				VersionStamp.Default,
				projectName,
				"ScriptAssembly",
				LanguageNames.CSharp,
				compilationOptions: compilationOptions,
				parseOptions: parseOptions,
				documents: new[] { documentInfo },
				isSubmission: true,
				hostObjectType: typeof(CSharpGlobals)
				));

			var success =_roslynWorkspace.TryApplyChanges(solution);
			Trace.Assert(success);

			_roslynWorkspace.OpenDocument(_documentId, sourceTextContainer);
		}

		public async Task<TextSpan?> FindDefinition(int position)
		{
			var document = _roslynWorkspace.CurrentSolution.GetDocument(_documentId);
			var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
			var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position);

			if (symbol != null)
			{
				// TODO: Handle multiple locations?
				var location = symbol.Locations.First();
				return location.SourceSpan;
			}

			return null;
		}
	}
}
