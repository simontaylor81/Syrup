using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ShaderEditorApp.Model.Editor.CSharp
{
	// Helper class for formatting C# code tips.
	internal static class CodeTipFormatter
	{
		private static SymbolDisplayFormat _displayFormat = new SymbolDisplayFormat(
				globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
				genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
				memberOptions:
					SymbolDisplayMemberOptions.IncludeParameters |
					SymbolDisplayMemberOptions.IncludeType |
					SymbolDisplayMemberOptions.IncludeContainingType,
				kindOptions:
					SymbolDisplayKindOptions.IncludeMemberKeyword,
				parameterOptions:
					SymbolDisplayParameterOptions.IncludeName |
					SymbolDisplayParameterOptions.IncludeType |
					SymbolDisplayParameterOptions.IncludeParamsRefOut |
					SymbolDisplayParameterOptions.IncludeDefaultValue,
				propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
				localOptions: SymbolDisplayLocalOptions.IncludeType,
				miscellaneousOptions:
					SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
					SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

		public static string FormatCodeTip(ISymbol symbol, DocumentationHelper documentationHelper)
		{
			// Combind kind-specific prefix with the (customised) display string.
			var result = GetPrefix(symbol) + symbol.ToDisplayString(_displayFormat);

			// Add documentation string if we have one.
			var docString = documentationHelper.GetDocumentationString(symbol);
			if (!string.IsNullOrEmpty(docString))
			{
				result += "\n" + docString;
			}

			return result;
		}

		// Get a prefix to prepend before the display string.
		private static string GetPrefix(ISymbol symbol)
		{
			switch (symbol.Kind)
			{
				case SymbolKind.Field: return "(field) ";
				case SymbolKind.Local: return "(local) ";
				case SymbolKind.Parameter: return "(parameter) ";
				case SymbolKind.NamedType: return GetNamedTypePrefix(symbol);
			}

			// Default to no prefix -- some things don't need one.
			return "";
		}

		private static string GetNamedTypePrefix(ISymbol symbol)
		{
			var namedTypeSymbol = (INamedTypeSymbol)symbol;
			switch (namedTypeSymbol.TypeKind)
			{
				case TypeKind.Class:
					return "class ";
				case TypeKind.Delegate:
					return "delegate ";
				case TypeKind.Dynamic:
					return "dynamic ";
				case TypeKind.Enum:
					return "enum ";
				case TypeKind.Interface:
					return "interface ";
				case TypeKind.Struct:
					return "struct ";
			}

			return "";
		}
	}
}
