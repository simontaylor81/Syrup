using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using SRPCommon.Util;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Documentation provider implementation that reads XML documentation from a file.
	// Basically a cut-down version of FileBasedXmlDocumentationProvider from Roslyn (because it's internal).
	internal class RoslynXmlDocumentationProvider : DocumentationProvider
	{
		private readonly string _path;
		private readonly Lazy<Dictionary<string, string>> _docComments;

		public RoslynXmlDocumentationProvider(string path)
		{
			_path = path;

		// We load the file lazily to speed up load times
		// (We may never need this, e.g. if you're using python scripts).
		_docComments = new Lazy<Dictionary<string, string>>(LoadDocComments);
		}

		protected override string GetDocumentationForSymbol(
			string documentationMemberID,
			CultureInfo preferredCulture,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			return _docComments.Value.GetOrDefault(documentationMemberID, "");
		}

		// Load and parse the contents of the file.
		private Dictionary<string, string> LoadDocComments()
		{
			var doc = XDocument.Load(_path);
			return doc.Descendants("member")
				.Where(element => element.Attribute("name") != null)
				.ToDictionary(
					element => element.Attribute("name").Value,
					element => string.Concat(element.Nodes()));
		}

		public override bool Equals(object obj)
		{
			var other = obj as RoslynXmlDocumentationProvider;
			return other != null && _path == other._path;
		}

		public override int GetHashCode() => _path.GetHashCode();
	}
}