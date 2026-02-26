#pragma warning disable CS8600, CS8604 // Null reference warnings suppressed: values are guaranteed non-null by attribute shape

using System.Collections.Generic;
using System.Collections.Immutable;
using Facette.Generator.Models;
using Microsoft.CodeAnalysis;

namespace Facette.Generator.Builders
{
    public static class ModelBuilder
    {
        public static FacetteTargetModel Build(GeneratorAttributeSyntaxContext context)
        {
            var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;

            var attribute = context.Attributes[0];

            // Extract source type
            var sourceType = (INamedTypeSymbol)attribute.ConstructorArguments[0].Value;

            // Extract exclude list from params
            var exclude = new HashSet<string>();
            if (attribute.ConstructorArguments.Length > 1 &&
                attribute.ConstructorArguments[1].Kind == TypedConstantKind.Array)
            {
                foreach (var val in attribute.ConstructorArguments[1].Values)
                {
                    if (val.Value is string s)
                    {
                        exclude.Add(s);
                    }
                }
            }

            // Extract Include named argument
            HashSet<string> include = null;
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
                        }
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
                generateMapper
            );
        }

        private static ImmutableArray<PropertyModel> GetSourceProperties(
            INamedTypeSymbol sourceType,
            HashSet<string> exclude,
            HashSet<string> include)
        {
            var builder = ImmutableArray.CreateBuilder<PropertyModel>();

            foreach (var member in sourceType.GetMembers())
            {
                if (!(member is IPropertySymbol prop))
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

            return builder.ToImmutable();
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
