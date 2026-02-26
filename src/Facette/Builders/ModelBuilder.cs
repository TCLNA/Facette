using System.Collections.Generic;
using System.Collections.Immutable;
using Facette.Generator.Diagnostics;
using Facette.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Facette.Generator.Builders
{
    public static class ModelBuilder
    {
        public static FacetteTargetModel Build(GeneratorAttributeSyntaxContext context)
        {
            var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
            var diagnosticsBuilder = ImmutableArray.CreateBuilder<DiagnosticInfo>();
            var locationObj = context.TargetNode.GetLocation();
            var filePath = locationObj.SourceTree != null ? locationObj.SourceTree.FilePath : "";
            var textSpan = locationObj.SourceSpan;
            var lineSpan = locationObj.GetLineSpan().Span;

            // FCT001: Type must be partial
            if (context.TargetNode is TypeDeclarationSyntax typeDecl
                && !typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                diagnosticsBuilder.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.FCT001_TypeMustBePartial,
                    filePath,
                    textSpan,
                    lineSpan,
                    new object[] { targetSymbol.Name }));
            }

            var attribute = context.Attributes[0];

            // Extract source type (defensive: null/error type if typeof() can't resolve)
            var sourceTypeValue = attribute.ConstructorArguments.Length > 0
                ? attribute.ConstructorArguments[0].Value as INamedTypeSymbol
                : null;

            if (sourceTypeValue == null || sourceTypeValue.TypeKind == TypeKind.Error)
            {
                diagnosticsBuilder.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.FCT003_SourceTypeNotFound,
                    filePath,
                    textSpan,
                    lineSpan,
                    new object[] { targetSymbol.Name }));

                return new FacetteTargetModel(
                    "",
                    targetSymbol.Name,
                    "",
                    ImmutableArray<PropertyModel>.Empty,
                    false,
                    false,
                    false,
                    diagnosticsBuilder.ToImmutable());
            }

            var sourceType = sourceTypeValue;

            // Extract exclude list from params
            var exclude = new HashSet<string>();
            bool hasExclude = false;
            if (attribute.ConstructorArguments.Length > 1 &&
                attribute.ConstructorArguments[1].Kind == TypedConstantKind.Array)
            {
                foreach (var val in attribute.ConstructorArguments[1].Values)
                {
                    if (val.Value is string s)
                    {
                        exclude.Add(s);
                        hasExclude = true;
                    }
                }
            }

            // Extract Include named argument
            HashSet<string>? include = null;
            bool hasInclude = false;
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == "Include" && arg.Value.Kind == TypedConstantKind.Array)
                {
                    include = new HashSet<string>();
                    foreach (var val in arg.Value.Values)
                    {
                        if (val.Value is string s)
                        {
                            include.Add(s);
                            hasInclude = true;
                        }
                    }
                }
            }

            // FCT002: Include and Exclude cannot both be specified
            if (hasExclude && hasInclude)
            {
                diagnosticsBuilder.Add(new DiagnosticInfo(
                    DiagnosticDescriptors.FCT002_IncludeExcludeConflict,
                    filePath,
                    textSpan,
                    lineSpan,
                    new object[] { targetSymbol.Name }));
            }

            // FCT004: Validate Include/Exclude property names exist on source type
            var sourcePropertyNames = GetSourcePropertyNames(sourceType);
            foreach (var name in exclude)
            {
                if (!sourcePropertyNames.Contains(name))
                {
                    diagnosticsBuilder.Add(new DiagnosticInfo(
                        DiagnosticDescriptors.FCT004_PropertyNotFound,
                        filePath,
                        textSpan,
                        lineSpan,
                        new object[] { name, sourceType.Name }));
                }
            }

            if (include != null)
            {
                foreach (var name in include)
                {
                    if (!sourcePropertyNames.Contains(name))
                    {
                        diagnosticsBuilder.Add(new DiagnosticInfo(
                            DiagnosticDescriptors.FCT004_PropertyNotFound,
                            filePath,
                            textSpan,
                            lineSpan,
                            new object[] { name, sourceType.Name }));
                    }
                }
            }

            // Extract generation flags
            bool generateToSource = GetNamedBoolArg(attribute, "GenerateToSource", true);
            bool generateProjection = GetNamedBoolArg(attribute, "GenerateProjection", true);
            bool generateMapper = GetNamedBoolArg(attribute, "GenerateMapper", true);

            // Collect properties from source type
            var properties = GetSourceProperties(sourceType, exclude, include);

            var ns = targetSymbol.ContainingNamespace.IsGlobalNamespace
                ? ""
                : targetSymbol.ContainingNamespace.ToDisplayString();

            return new FacetteTargetModel(
                ns,
                targetSymbol.Name,
                sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                properties,
                generateToSource,
                generateProjection,
                generateMapper,
                diagnosticsBuilder.ToImmutable()
            );
        }

        private static ImmutableArray<PropertyModel> GetSourceProperties(
            INamedTypeSymbol sourceType,
            HashSet<string> exclude,
            HashSet<string>? include)
        {
            var builder = ImmutableArray.CreateBuilder<PropertyModel>();
            var seen = new HashSet<string>();

            var current = sourceType;
            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var member in current.GetMembers())
                {
                    if (!(member is IPropertySymbol prop))
                    {
                        continue;
                    }

                    // Skip if already seen from a derived type (override)
                    if (!seen.Add(prop.Name))
                    {
                        continue;
                    }

                    if (prop.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    if (prop.IsStatic || prop.IsIndexer)
                    {
                        continue;
                    }

                    if (prop.GetMethod == null)
                    {
                        continue;
                    }

                    if (include != null && !include.Contains(prop.Name))
                    {
                        continue;
                    }

                    if (exclude.Contains(prop.Name))
                    {
                        continue;
                    }

                    var typeDisplay = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var isValueType = prop.Type.IsValueType;

                    builder.Add(new PropertyModel(prop.Name, typeDisplay, isValueType));
                }

                current = current.BaseType;
            }

            return builder.ToImmutable();
        }

        private static HashSet<string> GetSourcePropertyNames(INamedTypeSymbol sourceType)
        {
            var names = new HashSet<string>();
            var current = sourceType;
            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol prop
                        && prop.DeclaredAccessibility == Accessibility.Public
                        && !prop.IsStatic
                        && !prop.IsIndexer
                        && prop.GetMethod != null)
                    {
                        names.Add(prop.Name);
                    }
                }

                current = current.BaseType;
            }

            return names;
        }

        private static bool GetNamedBoolArg(AttributeData attribute, string name, bool defaultValue)
        {
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == name && arg.Value.Value is bool val)
                {
                    return val;
                }
            }

            return defaultValue;
        }
    }
}
