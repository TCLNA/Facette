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

            // Extract NestedDtos named argument
            var nestedDtoOverrides = new HashSet<string>();
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == "NestedDtos" && arg.Value.Kind == TypedConstantKind.Array)
                {
                    foreach (var val in arg.Value.Values)
                    {
                        if (val.Value is INamedTypeSymbol dtoType)
                        {
                            var dtoFullName = dtoType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            nestedDtoOverrides.Add(dtoFullName);
                        }
                    }
                }
            }

            // Build Facette lookup: scan all types in the compilation for [Facette] attributes
            var compilation = context.SemanticModel.Compilation;
            var allFacetteLookup = BuildFacetteLookup(compilation);
            var ownSourceFullName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var referencedSourceTypes = GetReferencedSourceTypes(sourceType, allFacetteLookup);
            var facetteLookup = BuildEffectiveLookup(allFacetteLookup, nestedDtoOverrides, ownSourceFullName, referencedSourceTypes, targetSymbol.Name, diagnosticsBuilder, filePath, textSpan, lineSpan);

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
                            convertContainingType: !string.IsNullOrEmpty(convertMethod) ? targetSymbol.Name : "",
                            flattenedNavigationType: resolveResult.Value.NavigationType ?? ""));
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
                            flattenedPathHasNullableSegment: flatResult.Value.HasNullableSegment,
                            flattenedNavigationType: flatResult.Value.NavigationType ?? ""));
                    }
                    // else: User-declared property without [MapFrom] and no flattening match — skip entirely
                }
            }

            // Collect source property names that are claimed by custom mappings or flattened navigation
            var claimedSourceNames = new HashSet<string>();
            foreach (var custom in customMappings)
            {
                if (custom.MappingKind == MappingKind.Custom && custom.SourcePropertyName != custom.Name)
                {
                    claimedSourceNames.Add(custom.SourcePropertyName);
                }
                else if (custom.MappingKind == MappingKind.Flattened && !string.IsNullOrEmpty(custom.FlattenedPath))
                {
                    // Claim the navigation property (first segment of the flattened path)
                    var dotIdx = custom.FlattenedPath.IndexOf('.');
                    if (dotIdx > 0)
                    {
                        claimedSourceNames.Add(custom.FlattenedPath.Substring(0, dotIdx));
                    }
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

            // Check if the DTO base type also has [Facette] — mark inherited properties
            var baseFacetteProperties = GetBaseFacettePropertyNames(targetSymbol);
            if (baseFacetteProperties.Count > 0)
            {
                var inheritedBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
                foreach (var prop in filteredBuilder)
                {
                    if (baseFacetteProperties.Contains(prop.Name))
                    {
                        // Mark as inherited — PropertyBuilder will skip it, but Mapping/Projection still use it
                        inheritedBuilder.Add(new PropertyModel(
                            prop.Name, prop.TypeFullName, prop.IsValueType,
                            prop.MappingKind, prop.SourcePropertyName,
                            prop.NestedDtoTypeName, prop.NestedDtoTypeFullName,
                            prop.CollectionElementTypeFullName,
                            prop.IsNullable, prop.IsArray, prop.NestedProperties,
                            prop.FlattenedPath, prop.FlattenedPathHasNullableSegment,
                            prop.ConvertMethod, prop.ConvertBackMethod, prop.ConvertContainingType,
                            prop.FlattenedNavigationType, prop.CollectionConvertMethod,
                            isInherited: true));
                    }
                    else
                    {
                        inheritedBuilder.Add(prop);
                    }
                }
                filteredBuilder = inheritedBuilder;
            }

            // Detect [AdditionalSource] attributes
            var additionalSourcesBuilder = ImmutableArray.CreateBuilder<AdditionalSourceInfo>();
            var existingPropNames = new HashSet<string>();
            foreach (var p in filteredBuilder)
                existingPropNames.Add(p.Name);

            foreach (var attrData in targetSymbol.GetAttributes())
            {
                if (attrData.AttributeClass == null
                    || attrData.AttributeClass.ToDisplayString() != "Facette.Abstractions.AdditionalSourceAttribute")
                    continue;

                if (attrData.ConstructorArguments.Length == 0)
                    continue;

                var additionalSourceType = attrData.ConstructorArguments[0].Value as INamedTypeSymbol;
                if (additionalSourceType == null || additionalSourceType.TypeKind == TypeKind.Error)
                    continue;

                var prefix = attrData.ConstructorArguments.Length > 1
                    && attrData.ConstructorArguments[1].Value is string p2
                    ? p2 : "";

                var addSourceFullName = additionalSourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var paramName = string.IsNullOrEmpty(prefix)
                    ? char.ToLowerInvariant(additionalSourceType.Name[0]) + additionalSourceType.Name.Substring(1)
                    : char.ToLowerInvariant(prefix[0]) + prefix.Substring(1);

                additionalSourcesBuilder.Add(new AdditionalSourceInfo(addSourceFullName, prefix, paramName));

                // Collect properties from additional source type (simple: direct mappings only)
                var addProps = GetSourcePropertyNames(additionalSourceType);
                var addCurrent = additionalSourceType;
                var addSeen = new HashSet<string>();
                while (addCurrent != null && addCurrent.SpecialType != SpecialType.System_Object)
                {
                    foreach (var member in addCurrent.GetMembers())
                    {
                        if (!(member is IPropertySymbol addProp)) continue;
                        if (!addSeen.Add(addProp.Name)) continue;
                        if (addProp.DeclaredAccessibility != Accessibility.Public) continue;
                        if (addProp.IsStatic || addProp.IsIndexer || addProp.GetMethod == null) continue;

                        var propDtoName = prefix + addProp.Name;
                        if (existingPropNames.Contains(propDtoName)) continue;
                        if (userDeclaredNames.Contains(propDtoName)) continue;

                        var typeDisplay = addProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        filteredBuilder.Add(new PropertyModel(
                            propDtoName, typeDisplay, addProp.Type.IsValueType,
                            MappingKind.Direct, addProp.Name,
                            "", "", "", false, false,
                            ImmutableArray<PropertyModel>.Empty,
                            sourceParameter: paramName));
                        existingPropNames.Add(propDtoName);
                    }
                    addCurrent = addCurrent.BaseType;
                }
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
                diagnosticsBuilder.ToImmutable(),
                hasBaseFacette: baseFacetteProperties.Count > 0,
                additionalSources: additionalSourcesBuilder.ToImmutable()
            );
        }

        private static Dictionary<string, List<FacetteDtoInfo>> BuildFacetteLookup(Compilation compilation)
        {
            var lookup = new Dictionary<string, List<FacetteDtoInfo>>();

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

                        List<FacetteDtoInfo> list;
                        if (!lookup.TryGetValue(sourceFullName, out list))
                        {
                            list = new List<FacetteDtoInfo>();
                            lookup[sourceFullName] = list;
                        }
                        list.Add(new FacetteDtoInfo(dtoTypeName, dtoTypeFullName, srcType));
                    }
                }
            }

            return lookup;
        }

        /// <summary>
        /// Collects the set of source type full names that are actually referenced
        /// as nested or collection element types from the given source type's properties.
        /// </summary>
        private static HashSet<string> GetReferencedSourceTypes(
            INamedTypeSymbol sourceType,
            Dictionary<string, List<FacetteDtoInfo>> allLookup)
        {
            var referenced = new HashSet<string>();
            var current = sourceType;
            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var member in current.GetMembers())
                {
                    if (!(member is IPropertySymbol prop)) continue;
                    if (prop.DeclaredAccessibility != Accessibility.Public) continue;
                    if (prop.IsStatic || prop.IsIndexer || prop.GetMethod == null) continue;

                    var propType = prop.Type;

                    // Unwrap Nullable<T>
                    if (propType is INamedTypeSymbol nt
                        && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        propType = nt.TypeArguments[0];
                    }

                    // Check collection element type
                    ITypeSymbol elementType = null;
                    if (propType.TypeKind == TypeKind.Array)
                    {
                        elementType = ((IArrayTypeSymbol)propType).ElementType;
                    }
                    else if (propType.SpecialType != SpecialType.System_String)
                    {
                        foreach (var iface in propType.AllInterfaces)
                        {
                            if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                                && iface.TypeArguments.Length == 1)
                            {
                                elementType = iface.TypeArguments[0];
                                break;
                            }
                        }
                    }

                    if (elementType != null)
                    {
                        var elemFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (allLookup.ContainsKey(elemFullName))
                            referenced.Add(elemFullName);
                    }
                    else
                    {
                        var typeFullName = propType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (allLookup.ContainsKey(typeFullName))
                            referenced.Add(typeFullName);
                    }
                }
                current = current.BaseType;
            }
            return referenced;
        }

        private static Dictionary<string, FacetteDtoInfo> BuildEffectiveLookup(
            Dictionary<string, List<FacetteDtoInfo>> allLookup,
            HashSet<string> nestedDtoOverrides,
            string ownSourceFullName,
            HashSet<string> referencedSourceTypes,
            string targetTypeName,
            ImmutableArray<DiagnosticInfo>.Builder diagnosticsBuilder,
            string filePath,
            Microsoft.CodeAnalysis.Text.TextSpan textSpan,
            Microsoft.CodeAnalysis.Text.LinePositionSpan lineSpan)
        {
            var effective = new Dictionary<string, FacetteDtoInfo>();

            foreach (var kvp in allLookup)
            {
                var sourceFullName = kvp.Key;
                var candidates = kvp.Value;

                if (candidates.Count == 1)
                {
                    effective[sourceFullName] = candidates[0];
                    continue;
                }

                // Multiple DTOs for same source type — check for override
                FacetteDtoInfo matched = null;
                foreach (var candidate in candidates)
                {
                    if (nestedDtoOverrides.Contains(candidate.DtoTypeFullName))
                    {
                        matched = candidate;
                        break;
                    }
                }

                if (matched != null)
                {
                    effective[sourceFullName] = matched;
                }
                else
                {
                    // Only emit FCT010 if this source type is actually referenced
                    // as a nested/collection property from the current target's source type
                    if (sourceFullName != ownSourceFullName && referencedSourceTypes.Contains(sourceFullName))
                    {
                        diagnosticsBuilder.Add(new DiagnosticInfo(
                            DiagnosticDescriptors.FCT010_AmbiguousNestedDto,
                            filePath, textSpan, lineSpan,
                            new object[] { sourceFullName, targetTypeName }));
                    }

                    // Use the first candidate as fallback
                    effective[sourceFullName] = candidates[0];
                }
            }

            return effective;
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

                    // Check for collection — use a preliminary element type for detection
                    var prelimCollInfo = DetectCollection(propType, "" /* placeholder */, isNullable);
                    if (prelimCollInfo != null)
                    {
                        var elemType = prelimCollInfo.Value.ElementType;
                        var elementFullName = elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        FacetteDtoInfo elementDtoInfo;
                        if (facetteLookup.TryGetValue(elementFullName, out elementDtoInfo))
                        {
                            // Re-detect with actual DTO element type for correct type names
                            var collInfo = DetectCollection(propType, elementDtoInfo.DtoTypeFullName, isNullable).Value;
                            var nestedProps = GetNestedSourceProperties(elementDtoInfo.SourceType, facetteLookup, new HashSet<string>(visited));

                            builder.Add(new PropertyModel(
                                prop.Name, collInfo.DtoTypeName, false,
                                MappingKind.Collection, prop.Name,
                                elementDtoInfo.DtoTypeName, elementDtoInfo.DtoTypeFullName,
                                elementFullName, isNullable, collInfo.IsArray, nestedProps,
                                collectionConvertMethod: collInfo.ConvertMethod));
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
                    var prelimCollInfo2 = DetectCollection(propType, "" /* placeholder */, isNullable);
                    if (prelimCollInfo2 != null)
                    {
                        var elemType2 = prelimCollInfo2.Value.ElementType;
                        var elementFullName = elemType2.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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

                        var collInfo2 = DetectCollection(propType, dtoElementType, isNullable).Value;

                        builder.Add(new PropertyModel(
                            prop.Name,
                            collInfo2.DtoTypeName,
                            false,
                            MappingKind.Collection,
                            prop.Name,
                            nestedDtoTypeName,
                            nestedDtoTypeFullName,
                            elementFullName,
                            isNullable,
                            collInfo2.IsArray,
                            nestedProps,
                            collectionConvertMethod: collInfo2.ConvertMethod));

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
            public string NavigationType; // fully-qualified type of the first navigation property
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

            // Determine the navigation type (type of the first segment)
            string navType = "";
            if (segments.Length > 1)
            {
                var firstProp = FindSourceProperty(sourceType, segments[0]);
                if (firstProp != null)
                {
                    var firstType = firstProp.Type;
                    if (firstType is INamedTypeSymbol fnt
                        && fnt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        firstType = fnt.TypeArguments[0];
                    }
                    navType = firstType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }

            return new FlattenedPathResult { Path = path, HasNullableSegment = hasNullable, LeafType = currentType, NavigationType = navType };
        }

        /// <summary>
        /// Attempts greedy prefix matching to resolve a property name like "AddressCity"
        /// to a flattened path "Address.City" on the source type.
        /// </summary>
        private static FlattenedPathResult? TryResolveFlattenedPath(INamedTypeSymbol sourceType, string propertyName)
        {
            var result = TryResolveFlattenedPathRecursive(sourceType, propertyName, "", false);
            if (result != null && result.Value.Path.Contains("."))
            {
                // Determine navigation type from first segment
                var firstSegment = result.Value.Path.Split('.')[0];
                var firstProp = FindSourceProperty(sourceType, firstSegment);
                if (firstProp != null)
                {
                    var navType = firstProp.Type;
                    if (navType is INamedTypeSymbol fnt
                        && fnt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        navType = fnt.TypeArguments[0];
                    }
                    var r = result.Value;
                    r.NavigationType = navType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    return r;
                }
            }
            return result;
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

        /// <summary>
        /// Checks if the DTO's base type (or any ancestor) has [Facette] and collects
        /// the property names from the base source type(s) that the base DTO would generate.
        /// </summary>
        private static HashSet<string> GetBaseFacettePropertyNames(INamedTypeSymbol targetSymbol)
        {
            var names = new HashSet<string>();
            var baseType = targetSymbol.BaseType;

            while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
            {
                foreach (var attr in baseType.GetAttributes())
                {
                    if (attr.AttributeClass != null
                        && attr.AttributeClass.ToDisplayString() == "Facette.Abstractions.FacetteAttribute"
                        && attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is INamedTypeSymbol baseSourceType)
                    {
                        // Collect public instance property names from the base source type
                        var baseSourceNames = GetSourcePropertyNames(baseSourceType);
                        foreach (var name in baseSourceNames)
                        {
                            names.Add(name);
                        }

                        // Also collect user-declared properties on the base DTO (custom mappings, flattened, etc.)
                        foreach (var member in baseType.GetMembers())
                        {
                            if (member is IPropertySymbol prop
                                && prop.DeclaredAccessibility == Accessibility.Public
                                && !prop.IsStatic
                                && !prop.IsIndexer)
                            {
                                names.Add(prop.Name);
                            }
                        }
                        break;
                    }
                }

                baseType = baseType.BaseType;
            }

            return names;
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

        private struct CollectionInfo
        {
            public ITypeSymbol ElementType;
            public bool IsArray;
            public string ConvertMethod; // e.g. ".ToList()", ".ToArray()", ".ToImmutableArray()"
            public string DtoTypeName; // fully-qualified DTO collection type, e.g. "List<DtoType>"
        }

        /// <summary>
        /// Detects the collection kind and returns element type, convert method, and DTO type format.
        /// Returns null if not a collection.
        /// </summary>
        private static CollectionInfo? DetectCollection(ITypeSymbol propType, string dtoElementType, bool isNullable)
        {
            if (propType.TypeKind == TypeKind.Array)
            {
                var elemType = ((IArrayTypeSymbol)propType).ElementType;
                var typeName = dtoElementType + "[]";
                if (isNullable) typeName += "?";
                return new CollectionInfo
                {
                    ElementType = elemType,
                    IsArray = true,
                    ConvertMethod = ".ToArray()",
                    DtoTypeName = typeName
                };
            }

            if (propType.SpecialType == SpecialType.System_String)
                return null;

            // Check for specific collection types
            var namedType = propType as INamedTypeSymbol;
            if (namedType == null)
                return null;

            var originalDef = namedType.OriginalDefinition.ToDisplayString();
            ITypeSymbol elementType = null;

            // Check AllInterfaces for IEnumerable<T> to get element type
            foreach (var iface in propType.AllInterfaces)
            {
                if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                    && iface.TypeArguments.Length == 1)
                {
                    elementType = iface.TypeArguments[0];
                    break;
                }
            }

            // Also check if the type itself is IEnumerable<T>
            if (elementType == null && namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                elementType = namedType.TypeArguments[0];
            }

            if (elementType == null)
                return null;

            // Determine the convert method and DTO type based on the collection type
            string convertMethod;
            string dtoCollectionTypeName;

            if (originalDef == "System.Collections.Generic.HashSet<T>"
                || originalDef == "System.Collections.Generic.ISet<T>")
            {
                convertMethod = ".ToHashSet()";
                dtoCollectionTypeName = "global::System.Collections.Generic.HashSet<" + dtoElementType + ">";
            }
            else if (originalDef == "System.Collections.Immutable.ImmutableArray<T>")
            {
                convertMethod = ".ToImmutableArray()";
                dtoCollectionTypeName = "global::System.Collections.Immutable.ImmutableArray<" + dtoElementType + ">";
            }
            else if (originalDef == "System.Collections.Immutable.ImmutableList<T>"
                || originalDef == "System.Collections.Immutable.IImmutableList<T>")
            {
                convertMethod = ".ToImmutableList()";
                dtoCollectionTypeName = "global::System.Collections.Immutable.ImmutableList<" + dtoElementType + ">";
            }
            else if (originalDef == "System.Collections.Generic.IReadOnlyList<T>"
                || originalDef == "System.Collections.Generic.IReadOnlyCollection<T>"
                || originalDef == "System.Collections.Generic.IEnumerable<T>")
            {
                // IReadOnlyList/IReadOnlyCollection/IEnumerable — produce List which implements all of them
                convertMethod = ".ToList()";
                dtoCollectionTypeName = "global::System.Collections.Generic.List<" + dtoElementType + ">";
            }
            else
            {
                // Default: List<T> or any other IEnumerable<T> implementation
                convertMethod = ".ToList()";
                dtoCollectionTypeName = "global::System.Collections.Generic.List<" + dtoElementType + ">";
            }

            if (isNullable) dtoCollectionTypeName += "?";

            return new CollectionInfo
            {
                ElementType = elementType,
                IsArray = false,
                ConvertMethod = convertMethod,
                DtoTypeName = dtoCollectionTypeName
            };
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
