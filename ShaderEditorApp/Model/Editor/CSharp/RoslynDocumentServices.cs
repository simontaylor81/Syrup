using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
	public class RoslynDocumentServices : ICodeTipProvider
	{
		private readonly DocumentId _documentId;
		private readonly ProjectId _projectId;
		private readonly RoslynScriptWorkspace _roslynWorkspace;

		public RoslynDocumentServices(SourceTextContainer sourceTextContainer, string path)
		{
			// Create a Roslyn workspace.
			// Is adhoc workspace sufficient?
			_roslynWorkspace = new RoslynScriptWorkspace();

			// TODO: Share this list with the scripting system.
			var usings = new[]
			{
				"System",
				"System.Linq",
				"System.Collections.Generic",
				"System.Numerics",
				"SRPScripting",
			};

			// TODO: Source file resolver.
			var compilationOptions = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				usings: usings,
				sourceReferenceResolver: SourceFileResolver.Default);

			var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script);

			var documentName = Path.GetFileName(path);
			var projectName = documentName + ".project";

			_projectId = ProjectId.CreateNewId(projectName);
			_documentId = DocumentId.CreateNewId(_projectId, documentName);

			// TODO: Share this list with the scripting system.
			var imports = new[]
			{
				typeof(object).Assembly,							// mscorlib.dll
				typeof(Uri).Assembly,								// System.
				typeof(Enumerable).Assembly,						// System.Core
				typeof(SRPScripting.IRenderInterface).Assembly,
				typeof(CSharpGlobals).Assembly,
				typeof(System.Numerics.Vector3).Assembly,
			};
			// TODO: This stuff is fairly expensive and should be shared by all documents!
			var metadataReferences = imports.Select(a => MetadataReference.CreateFromFile(a.Location));

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
				metadataReferences: metadataReferences,
				isSubmission: true,
				hostObjectType: typeof(CSharpGlobals)
				));

			_roslynWorkspace.SetSolution(solution);

			_roslynWorkspace.OpenDocument(_documentId, sourceTextContainer);
		}

		public async Task<TextSpan?> FindDefinitionAsync(int position)
		{
			var symbol = await GetSymbol(position);
			if (symbol != null)
			{
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
				return symbol.ToDisplayString();
			}

			return null;
		}

		public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(CancellationToken cancellationToken)
		{
			var document = _roslynWorkspace.CurrentSolution.GetDocument(_documentId);
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			return semanticModel.GetDiagnostics();
		}

		private Task<ISymbol> GetSymbol(int position)
		{
			var document = _roslynWorkspace.CurrentSolution.GetDocument(_documentId);
			return SymbolFinder.FindSymbolAtPositionAsync(document, position);
		}
	}
}
