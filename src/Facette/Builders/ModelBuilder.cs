using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            HashSet<string> include = null;
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

            // Build Facette lookup: scan all types in the compilation for [Facette] attributes
            var compilation = context.SemanticModel.Compilation;
            var facetteLookup = BuildFacetteLookup(compilation);

            // Collect properties from source type
            var autoProperties = GetSourceProperties(sourceType, exclude, include, facetteLookup, compilation);

            // Scan user-declared properties on the target type for [MapFrom] attributes
            var userDeclaredNames = new HashSet<string>();
            var customMappings = ImmutableArray.CreateBuilder<PropertyModel>();

            foreach (var member in targetSymbol.GetMembers())
            {
                if (!(member is IPropertySymbol targetProp))
                {
                    continue;
                }

                if (targetProp.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (targetProp.IsStatic || targetProp.IsIndexer)
                {
                    continue;
                }

                // Track all user-declared property names so they are not re-generated
                userDeclaredNames.Add(targetProp.Name);

                // Check for [FacetteIgnore] attribute — exclude from mapping entirely
                bool isIgnored = false;
                foreach (var attrData in targetProp.GetAttributes())
                {
                    if (attrData.AttributeClass != null
                        && attrData.AttributeClass.ToDisplayString() == "Facette.Abstractions.FacetteIgnoreAttribute")
                    {
                        isIgnored = true;
                        break;
                    }
                }

                if (isIgnored)
                {
                    // Property is in userDeclaredNames (prevents auto-gen) but NOT in customMappings (no mapping)
                    continue;
                }

                // Check for [MapFrom] attribute
                AttributeData mapFromAttr = null;
                foreach (var attrData in targetProp.GetAttributes())
                {
                    if (attrData.AttributeClass != null
                        && attrData.AttributeClass.ToDisplayString() == "Facette.Abstractions.MapFromAttribute")
                    {
                        mapFromAttr = attrData;
                        break;
                    }
                }

                if (mapFromAttr != null)
                {
                    // Extract source property name from constructor arg (may be null for parameterless ctor)
                    string mapFromSource = null;
                    if (mapFromAttr.ConstructorArguments.Length > 0
                        && mapFromAttr.ConstructorArguments[0].Value is string srcName)
                    {
                        mapFromSource = srcName;
                    }

                    // Extract Convert/ConvertBack named args
                    string convertMethod = null;
                    string convertBackMethod = null;
                    foreach (var namedArg in mapFromAttr.NamedArguments)
                    {
                        if (namedArg.Key == "Convert" && namedArg.Value.Value is string cm)
                            convertMethod = cm;
                        else if (namedArg.Key == "ConvertBack" && namedArg.Value.Value is string cbm)
                            convertBackMethod = cbm;
                    }

                    // If no source name given (parameterless), use property's own name
                    if (mapFromSource == null)
                    {
                        mapFromSource = targetProp.Name;
                    }

                    // Check for dot-notation path (e.g., "Address.City")
                    if (mapFromSource.Contains("."))
                    {
                        var resolveResult = ResolvePropertyPath(sourceType, mapFromSource);
                        if (resolveResult == null)
                        {
                            // Find the failing segment for FCT007
                            var segments = mapFromSource.Split('.');
                            var walkType = sourceType;
                            foreach (var seg in segments)
                            {
                                var segProp = FindSourceProperty(walkType, seg);
                                if (segProp == null)
                                {
                                    diagnosticsBuilder.Add(new DiagnosticInfo(
                                        DiagnosticDescriptors.FCT007_FlattenedPathSegmentNotFound,
                                        filePath, textSpan, lineSpan,
                                        new object[] { seg, targetProp.Name, walkType.Name }));
                                    break;
                                }
                                walkType = segProp.Type as INamedTypeSymbol;
                                if (walkType == null) break;
                            }
                            continue;
                        }

                        var flatTypeDisplay = resolveResult.Value.LeafType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        customMappings.Add(new PropertyModel(
                            targetProp.Name,
                            flatTypeDisplay,
                            resolveResult.Value.LeafType.IsValueType,
                            MappingKind.Flattened,
                            targetProp.Name,
                            "", "", "",
                            false, false,
                            ImmutableArray<PropertyModel>.Empty,
                            flattenedPath: mapFromSource,
                            flattenedPathHasNullableSegment: resolveResult.Value.HasNullableSegment,
                            convertMethod: convertMethod ?? "",
                            convertBackMethod: convertBackMethod ?? "",
                            convertContainingType: !string.IsNullOrEmpty(convertMethod) ? targetSymbol.Name : ""));
                    }
                    else
                    {
                        // Simple MapFrom — validate source property exists
                        var sourceProp = FindSourceProperty(sourceType, mapFromSource);
                        if (sourceProp == null)
                        {
                            diagnosticsBuilder.Add(new DiagnosticInfo(
                                DiagnosticDescriptors.FCT005_MapFromPropertyNotFound,
                                filePath, textSpan, lineSpan,
                                new object[] { targetProp.Name, mapFromSource, sourceType.Name }));
                            continue;
                        }

                        // Validate Convert/ConvertBack methods if specified
                        if (!string.IsNullOrEmpty(convertMethod) && !HasStaticMethod(targetSymbol, convertMethod))
                        {
                            diagnosticsBuilder.Add(new DiagnosticInfo(
                                DiagnosticDescriptors.FCT008_ConvertMethodNotFound,
                                filePath, textSpan, lineSpan,
                                new object[] { convertMethod, targetProp.Name, targetSymbol.Name }));
                            convertMethod = null;
                        }
                        if (!string.IsNullOrEmpty(convertBackMethod) && !HasStaticMethod(targetSymbol, convertBackMethod))
                        {
                            diagnosticsBuilder.Add(new DiagnosticInfo(
                                DiagnosticDescriptors.FCT009_ConvertBackMethodNotFound,
                                filePath, textSpan, lineSpan,
                                new object[] { convertBackMethod, targetProp.Name, targetSymbol.Name }));
                            convertBackMethod = null;
                        }

                        var typeDisplay = targetProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var isValueType = targetProp.Type.IsValueType;

                        customMappings.Add(new PropertyModel(
                            targetProp.Name,
                            typeDisplay,
                            isValueType,
                            MappingKind.Custom,
                            mapFromSource,
                            "", "", "",
                            false, false,
                            ImmutableArray<PropertyModel>.Empty,
                            convertMethod: convertMethod ?? "",
                            convertBackMethod: convertBackMethod ?? "",
                            convertContainingType: !string.IsNullOrEmpty(convertMethod) ? targetSymbol.Name : ""));
                    }
                }
                else
                {
                    // No [MapFrom] — attempt convention-based flattening
                    var flatResult = TryResolveFlattenedPath(sourceType, targetProp.Name);
                    if (flatResult != null)
                    {
                        var flatTypeDisplay = flatResult.Value.LeafType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        customMappings.Add(new PropertyModel(
                            targetProp.Name,
                            flatTypeDisplay,
                            flatResult.Value.LeafType.IsValueType,
                            MappingKind.Flattened,
                            targetProp.Name,
                            "", "", "",
                            false, false,
                            ImmutableArray<PropertyModel>.Empty,
                            flattenedPath: flatResult.Value.Path,
                            flattenedPathHasNullableSegment: flatResult.Value.HasNullableSegment));
                    }
                    // else: User-declared property without [MapFrom] and no flattening match — skip entirely
                }
            }

            // Collect source property names that are claimed by custom mappings
            var claimedSourceNames = new HashSet<string>();
            foreach (var custom in customMappings)
            {
                if (custom.MappingKind == MappingKind.Custom && custom.SourcePropertyName != custom.Name)
                {
                    claimedSourceNames.Add(custom.SourcePropertyName);
                }
            }

            // Filter auto-generated properties — remove any whose name matches a user-declared property
            // or whose source property is claimed by a custom mapping
            var filteredBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
            foreach (var prop in autoProperties)
            {
                if (userDeclaredNames.Contains(prop.Name))
                    continue;
                if (claimedSourceNames.Contains(prop.Name))
                    continue;
                filteredBuilder.Add(prop);
            }

            // Merge: filtered auto-generated + custom mappings
            foreach (var custom in customMappings)
            {
                filteredBuilder.Add(custom);
            }

            var properties = filteredBuilder.ToImmutable();

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

        private static Dictionary<string, FacetteDtoInfo> BuildFacetteLookup(Compilation compilation)
        {
            var lookup = new Dictionary<string, FacetteDtoInfo>();

            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();

                foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
                    if (typeSymbol == null)
                    {
                        continue;
                    }

                    foreach (var attrData in typeSymbol.GetAttributes())
                    {
                        if (attrData.AttributeClass == null)
                        {
                            continue;
                        }

                        if (attrData.AttributeClass.ToDisplayString() != "Facette.Abstractions.FacetteAttribute")
                        {
                            continue;
                        }

                        if (attrData.ConstructorArguments.Length == 0)
                        {
                            continue;
                        }

                        var srcType = attrData.ConstructorArguments[0].Value as INamedTypeSymbol;
                        if (srcType == null || srcType.TypeKind == TypeKind.Error)
                        {
                            continue;
                        }

                        var sourceFullName = srcType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var dtoTypeName = typeSymbol.Name;
                        var dtoTypeFullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        lookup[sourceFullName] = new FacetteDtoInfo(dtoTypeName, dtoTypeFullName, srcType);
                    }
                }
            }

            return lookup;
        }

        /// <summary>
        /// Recursively gets source properties for projection inlining, resolving nested DTOs and collections.
        /// Uses visited set for cycle detection.
        /// </summary>
        private static ImmutableArray<PropertyModel> GetNestedSourceProperties(
            INamedTypeSymbol sourceType,
            Dictionary<string, FacetteDtoInfo> facetteLookup,
            HashSet<string> visited)
        {
            var sourceFullName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!visited.Add(sourceFullName))
            {
                // Circular reference — return empty to break the cycle
                return ImmutableArray<PropertyModel>.Empty;
            }

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

                    // Determine nullability
                    var propType = prop.Type;
                    bool isNullable = false;
                    if (propType is INamedTypeSymbol nt
                        && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        isNullable = true;
                        propType = nt.TypeArguments[0];
                    }
                    else if (prop.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        isNullable = true;
                    }

                    // Check for collection
                    ITypeSymbol collectionElementType = null;
                    bool isArray = false;
                    if (prop.Type.TypeKind == TypeKind.Array)
                    {
                        isArray = true;
                        collectionElementType = ((IArrayTypeSymbol)prop.Type).ElementType;
                    }
                    else if (prop.Type.SpecialType != SpecialType.System_String)
                    {
                        foreach (var iface in prop.Type.AllInterfaces)
                        {
                            if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                                && iface.TypeArguments.Length == 1)
                            {
                                collectionElementType = iface.TypeArguments[0];
                                break;
                            }
                        }
                    }

                    if (collectionElementType != null)
                    {
                        var elementFullName = collectionElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        FacetteDtoInfo elementDtoInfo;
                        if (facetteLookup.TryGetValue(elementFullName, out elementDtoInfo))
                        {
                            var nestedProps = GetNestedSourceProperties(elementDtoInfo.SourceType, facetteLookup, new HashSet<string>(visited));
                            var dtoElementType = elementDtoInfo.DtoTypeFullName;
                            string collectionTypeName = isArray
                                ? dtoElementType + "[]"
                                : "global::System.Collections.Generic.List<" + dtoElementType + ">";
                            if (isNullable) collectionTypeName += "?";

                            builder.Add(new PropertyModel(
                                prop.Name, collectionTypeName, false,
                                MappingKind.Collection, prop.Name,
                                elementDtoInfo.DtoTypeName, elementDtoInfo.DtoTypeFullName,
                                elementFullName, isNullable, isArray, nestedProps));
                        }
                        else
                        {
                            var typeDisplay = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            builder.Add(PropertyModel.Direct(prop.Name, typeDisplay, prop.Type.IsValueType));
                        }
                        continue;
                    }

                    // Check for nested DTO
                    var unwrappedFullName = propType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    FacetteDtoInfo dtoInfo;
                    if (facetteLookup.TryGetValue(unwrappedFullName, out dtoInfo))
                    {
                        var nestedProps = GetNestedSourceProperties(dtoInfo.SourceType, facetteLookup, new HashSet<string>(visited));
                        var dtoTypeForProperty = dtoInfo.DtoTypeFullName;
                        if (isNullable) dtoTypeForProperty += "?";

                        builder.Add(new PropertyModel(
                            prop.Name, dtoTypeForProperty, false,
                            MappingKind.Nested, prop.Name,
                            dtoInfo.DtoTypeName, dtoInfo.DtoTypeFullName,
                            "", isNullable, false, nestedProps));
                    }
                    else
                    {
                        var typeDisplay = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        builder.Add(PropertyModel.Direct(prop.Name, typeDisplay, prop.Type.IsValueType));
                    }
                }

                current = current.BaseType;
            }

            visited.Remove(sourceFullName);
            return builder.ToImmutable();
        }

        private static ImmutableArray<PropertyModel> GetSourceProperties(
            INamedTypeSymbol sourceType,
            HashSet<string> exclude,
            HashSet<string> include,
            Dictionary<string, FacetteDtoInfo> facetteLookup,
            Compilation compilation)
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

                    // Determine nullability and unwrap the underlying type
                    var propType = prop.Type;
                    bool isNullable = false;

                    // Check for Nullable<T> value types
                    if (propType is INamedTypeSymbol namedType
                        && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        isNullable = true;
                        propType = namedType.TypeArguments[0];
                    }
                    // Check for nullable reference types
                    else if (prop.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        isNullable = true;
                    }

                    // Check if the property is a collection type
                    ITypeSymbol collectionElementType = null;
                    bool isArray = false;

                    if (prop.Type.TypeKind == TypeKind.Array)
                    {
                        isArray = true;
                        collectionElementType = ((IArrayTypeSymbol)prop.Type).ElementType;
                    }
                    else if (prop.Type.SpecialType != SpecialType.System_String)
                    {
                        // Check AllInterfaces for IEnumerable<T>
                        foreach (var iface in prop.Type.AllInterfaces)
                        {
                            if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                                && iface.TypeArguments.Length == 1)
                            {
                                collectionElementType = iface.TypeArguments[0];
                                break;
                            }
                        }
                    }

                    if (collectionElementType != null)
                    {
                        // This is a collection property
                        var elementFullName = collectionElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        // Check if the element type has a Facette DTO
                        FacetteDtoInfo elementDtoInfo;
                        string dtoElementType;
                        string nestedDtoTypeName;
                        string nestedDtoTypeFullName;
                        ImmutableArray<PropertyModel> nestedProps;

                        if (facetteLookup.TryGetValue(elementFullName, out elementDtoInfo))
                        {
                            dtoElementType = elementDtoInfo.DtoTypeFullName;
                            nestedDtoTypeName = elementDtoInfo.DtoTypeName;
                            nestedDtoTypeFullName = elementDtoInfo.DtoTypeFullName;
                            nestedProps = GetNestedSourceProperties(elementDtoInfo.SourceType, facetteLookup, new HashSet<string>());
                        }
                        else
                        {
                            dtoElementType = elementFullName;
                            nestedDtoTypeName = "";
                            nestedDtoTypeFullName = "";
                            nestedProps = ImmutableArray<PropertyModel>.Empty;
                        }

                        // Build the DTO type name for the property
                        string collectionTypeName;
                        if (isArray)
                        {
                            collectionTypeName = dtoElementType + "[]";
                        }
                        else
                        {
                            collectionTypeName = "global::System.Collections.Generic.List<" + dtoElementType + ">";
                        }

                        if (isNullable)
                        {
                            collectionTypeName = collectionTypeName + "?";
                        }

                        builder.Add(new PropertyModel(
                            prop.Name,
                            collectionTypeName,
                            false, // collections are reference types
                            MappingKind.Collection,
                            prop.Name,
                            nestedDtoTypeName,
                            nestedDtoTypeFullName,
                            elementFullName,
                            isNullable,
                            isArray,
                            nestedProps));

                        continue;
                    }

                    // Check if the (unwrapped) property type has a corresponding Facette DTO
                    var unwrappedFullName = propType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    FacetteDtoInfo dtoInfo;
                    if (facetteLookup.TryGetValue(unwrappedFullName, out dtoInfo))
                    {
                        // This is a nested DTO property
                        var dtoTypeForProperty = dtoInfo.DtoTypeFullName;
                        if (isNullable)
                        {
                            dtoTypeForProperty = dtoTypeForProperty + "?";
                        }

                        var nestedProps = GetNestedSourceProperties(dtoInfo.SourceType, facetteLookup, new HashSet<string>());

                        builder.Add(new PropertyModel(
                            prop.Name,
                            dtoTypeForProperty,
                            false, // DTOs (records) are reference types
                            MappingKind.Nested,
                            prop.Name,
                            dtoInfo.DtoTypeName,
                            dtoInfo.DtoTypeFullName,
                            "",
                            isNullable,
                            false,
                            nestedProps));
                    }
                    else
                    {
                        var typeDisplay = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var isValueType = prop.Type.IsValueType;

                        builder.Add(PropertyModel.Direct(prop.Name, typeDisplay, isValueType));
                    }
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

        private static IPropertySymbol FindSourceProperty(INamedTypeSymbol sourceType, string propertyName)
        {
            var current = sourceType;
            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol prop
                        && prop.Name == propertyName
                        && prop.DeclaredAccessibility == Accessibility.Public
                        && !prop.IsStatic
                        && !prop.IsIndexer
                        && prop.GetMethod != null)
                    {
                        return prop;
                    }
                }

                current = current.BaseType;
            }

            return null;
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

        private struct FlattenedPathResult
        {
            public string Path;
            public bool HasNullableSegment;
            public ITypeSymbol LeafType;
        }

        /// <summary>
        /// Resolves a dot-notation path (e.g., "Address.City") against a source type.
        /// Returns null if any segment is invalid.
        /// </summary>
        private static FlattenedPathResult? ResolvePropertyPath(INamedTypeSymbol sourceType, string path)
        {
            var segments = path.Split('.');
            ITypeSymbol currentType = sourceType;
            bool hasNullable = false;

            for (int i = 0; i < segments.Length; i++)
            {
                var namedType = currentType as INamedTypeSymbol;
                if (namedType == null) return null;

                var prop = FindSourceProperty(namedType, segments[i]);
                if (prop == null) return null;

                // Check nullability of intermediate segments (not the leaf)
                if (i < segments.Length - 1)
                {
                    if (prop.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        hasNullable = true;
                    }
                }

                currentType = prop.Type;

                // Unwrap Nullable<T>
                if (currentType is INamedTypeSymbol nt
                    && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    hasNullable = true;
                    currentType = nt.TypeArguments[0];
                }
            }

            return new FlattenedPathResult { Path = path, HasNullableSegment = hasNullable, LeafType = currentType };
        }

        /// <summary>
        /// Attempts greedy prefix matching to resolve a property name like "AddressCity"
        /// to a flattened path "Address.City" on the source type.
        /// </summary>
        private static FlattenedPathResult? TryResolveFlattenedPath(INamedTypeSymbol sourceType, string propertyName)
        {
            return TryResolveFlattenedPathRecursive(sourceType, propertyName, "", false);
        }

        private static FlattenedPathResult? TryResolveFlattenedPathRecursive(
            INamedTypeSymbol currentType, string remaining, string pathSoFar, bool hasNullable)
        {
            if (string.IsNullOrEmpty(remaining)) return null;

            // Greedy: try longest prefix first
            for (int len = remaining.Length; len >= 1; len--)
            {
                var candidateName = remaining.Substring(0, len);
                var prop = FindSourceProperty(currentType, candidateName);
                if (prop == null) continue;

                var newPath = string.IsNullOrEmpty(pathSoFar)
                    ? candidateName
                    : pathSoFar + "." + candidateName;

                var rest = remaining.Substring(len);

                if (rest.Length == 0)
                {
                    // This is the leaf
                    return new FlattenedPathResult
                    {
                        Path = newPath,
                        HasNullableSegment = hasNullable,
                        LeafType = prop.Type
                    };
                }

                // There's more to resolve — this must be a navigation property
                var propType = prop.Type;
                bool segNullable = hasNullable || prop.NullableAnnotation == NullableAnnotation.Annotated;

                // Unwrap Nullable<T>
                if (propType is INamedTypeSymbol nt
                    && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    segNullable = true;
                    propType = nt.TypeArguments[0];
                }

                var namedPropType = propType as INamedTypeSymbol;
                if (namedPropType == null) continue;

                var result = TryResolveFlattenedPathRecursive(namedPropType, rest, newPath, segNullable);
                if (result != null) return result;
            }

            return null;
        }

        private static bool HasStaticMethod(INamedTypeSymbol type, string methodName)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is IMethodSymbol method
                    && method.IsStatic
                    && method.Name == methodName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Holds information about a discovered Facette DTO for nested mapping.
        /// </summary>
        private sealed class FacetteDtoInfo
        {
            public FacetteDtoInfo(string dtoTypeName, string dtoTypeFullName, INamedTypeSymbol sourceType)
            {
                DtoTypeName = dtoTypeName;
                DtoTypeFullName = dtoTypeFullName;
                SourceType = sourceType;
            }

            public string DtoTypeName { get; }
            public string DtoTypeFullName { get; }
            public INamedTypeSymbol SourceType { get; }
        }
    }
}
