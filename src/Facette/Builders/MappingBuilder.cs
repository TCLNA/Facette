using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders
{
    public static class MappingBuilder
    {
        public static string BuildFromSource(
            string dtoTypeName,
            string sourceTypeFullName,
            ImmutableArray<PropertyModel> properties,
            bool hasBaseFacette = false,
            ImmutableArray<AdditionalSourceInfo> additionalSources = default)
        {
            var sb = new StringBuilder();
            var addSources = additionalSources.IsDefault ? ImmutableArray<AdditionalSourceInfo>.Empty : additionalSources;

            var paramList = sourceTypeFullName + " source";
            foreach (var addSrc in addSources)
            {
                paramList += ", " + addSrc.SourceTypeFullName + " " + addSrc.ParameterName;
            }
            sb.AppendLine("    public static " + dtoTypeName + " FromSource(" + paramList + ")");
            sb.AppendLine("    {");
            sb.AppendLine("        var result = new " + dtoTypeName);
            sb.AppendLine("        {");

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var comma = i < properties.Length - 1 ? "," : "";
                var srcRef = string.IsNullOrEmpty(prop.SourceParameter) ? "source" : prop.SourceParameter;

                if (prop.MappingKind == MappingKind.Flattened)
                {
                    var flatExpr = ProjectionBuilder.BuildFlattenedExpression(srcRef, prop);
                    sb.AppendLine("            " + prop.Name + " = " + flatExpr + comma);
                }
                else if (prop.MappingKind == MappingKind.Collection)
                {
                    var collSourceName = prop.SourcePropertyName;
                    var toMethod = !string.IsNullOrEmpty(prop.CollectionConvertMethod) ? prop.CollectionConvertMethod : (prop.IsArray ? ".ToArray()" : ".ToList()");
                    string collExpr;
                    if (!string.IsNullOrEmpty(prop.NestedDtoTypeFullName))
                    {
                        collExpr = srcRef + "." + collSourceName + ".Select(x => " + prop.NestedDtoTypeFullName + ".FromSource(x))" + toMethod;
                    }
                    else
                    {
                        collExpr = srcRef + "." + collSourceName + toMethod;
                    }

                    string assignment;
                    if (prop.IsNullable)
                    {
                        assignment = prop.Name + " = " + srcRef + "." + collSourceName + " != null ? " + collExpr + " : null";
                    }
                    else
                    {
                        assignment = prop.Name + " = " + collExpr;
                    }
                    sb.AppendLine("            " + assignment + comma);
                }
                else if (prop.MappingKind == MappingKind.Nested)
                {
                    if (prop.IsNullable)
                    {
                        sb.AppendLine("            " + prop.Name + " = " + srcRef + "." + prop.Name + " != null ? " + prop.NestedDtoTypeFullName + ".FromSource(" + srcRef + "." + prop.Name + ") : null" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + prop.Name + " = " + prop.NestedDtoTypeFullName + ".FromSource(" + srcRef + "." + prop.Name + ")" + comma);
                    }
                }
                else
                {
                    var sourceName = (prop.MappingKind == MappingKind.Custom || !string.IsNullOrEmpty(prop.SourceParameter))
                        ? prop.SourcePropertyName : prop.Name;
                    var convertMethod = prop.ConvertMethod;
                    if (!string.IsNullOrEmpty(convertMethod))
                    {
                        sb.AppendLine("            " + prop.Name + " = " + prop.ConvertContainingType + "." + convertMethod + "(" + srcRef + "." + sourceName + ")" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + prop.Name + " = " + srcRef + "." + sourceName + comma);
                    }
                }
            }

            sb.AppendLine("        };");
            var hookArgs = "source";
            foreach (var addSrc in addSources)
            {
                hookArgs += ", " + addSrc.ParameterName;
            }
            sb.AppendLine("        result.OnAfterFromSource(" + hookArgs + ");");
            sb.AppendLine("        return result;");
            sb.AppendLine("    }");

            return sb.ToString().TrimEnd();
        }

        public static string BuildToSource(
            string sourceTypeFullName,
            ImmutableArray<PropertyModel> properties,
            bool hasBaseFacette = false)
        {
            // Separate flattened properties from regular ones; skip additional source properties
            var regularProperties = ImmutableArray.CreateBuilder<PropertyModel>();
            var flattenedProperties = new List<PropertyModel>();
            foreach (var prop in properties)
            {
                // Skip properties from additional sources — can't reverse-map
                if (!string.IsNullOrEmpty(prop.SourceParameter))
                    continue;
                if (prop.MappingKind == MappingKind.Flattened)
                {
                    flattenedProperties.Add(prop);
                    continue;
                }
                if (!string.IsNullOrEmpty(prop.ConvertMethod) && string.IsNullOrEmpty(prop.ConvertBackMethod))
                    continue;
                regularProperties.Add(prop);
            }

            // Group flattened properties by their first path segment (navigation property name)
            var flattenedGroups = new Dictionary<string, List<PropertyModel>>();
            foreach (var prop in flattenedProperties)
            {
                var dotIdx = prop.FlattenedPath.IndexOf('.');
                if (dotIdx < 0) continue;
                var navPropName = prop.FlattenedPath.Substring(0, dotIdx);
                List<PropertyModel> list;
                if (!flattenedGroups.TryGetValue(navPropName, out list))
                {
                    list = new List<PropertyModel>();
                    flattenedGroups[navPropName] = list;
                }
                list.Add(prop);
            }

            // Remove flattened groups that conflict with regular nested/direct properties
            var regularPropNames = new HashSet<string>();
            foreach (var prop in regularProperties)
            {
                var name = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                regularPropNames.Add(name);
            }
            var validGroups = new List<KeyValuePair<string, List<PropertyModel>>>();
            foreach (var kvp in flattenedGroups)
            {
                if (!regularPropNames.Contains(kvp.Key))
                    validGroups.Add(kvp);
            }

            // Compute total items for comma logic
            var totalItems = regularProperties.Count + validGroups.Count;
            var itemIndex = 0;

            var sb = new StringBuilder();

            var newKw = hasBaseFacette ? "new " : "";
            sb.AppendLine("    public " + newKw + sourceTypeFullName + " ToSource()");
            sb.AppendLine("    {");
            sb.AppendLine("        OnBeforeToSource();");
            sb.AppendLine("        var result = new " + sourceTypeFullName);
            sb.AppendLine("        {");

            for (int i = 0; i < regularProperties.Count; i++)
            {
                var prop = regularProperties[i];
                itemIndex++;
                var comma = itemIndex < totalItems ? "," : "";

                if (prop.MappingKind == MappingKind.Collection)
                {
                    var collTarget = prop.SourcePropertyName;
                    var toMethodR = !string.IsNullOrEmpty(prop.CollectionConvertMethod) ? prop.CollectionConvertMethod : (prop.IsArray ? ".ToArray()" : ".ToList()");
                    string collExprR;
                    if (!string.IsNullOrEmpty(prop.NestedDtoTypeFullName))
                    {
                        collExprR = "this." + prop.Name + ".Select(x => x.ToSource())" + toMethodR;
                    }
                    else
                    {
                        collExprR = "this." + prop.Name + toMethodR;
                    }

                    string assignment;
                    if (prop.IsNullable)
                    {
                        assignment = collTarget + " = this." + prop.Name + " != null ? " + collExprR + " : null";
                    }
                    else
                    {
                        assignment = collTarget + " = " + collExprR;
                    }
                    sb.AppendLine("            " + assignment + comma);
                }
                else if (prop.MappingKind == MappingKind.Nested)
                {
                    if (prop.IsNullable)
                    {
                        sb.AppendLine("            " + prop.Name + " = this." + prop.Name + " != null ? this." + prop.Name + ".ToSource() : null" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + prop.Name + " = this." + prop.Name + ".ToSource()" + comma);
                    }
                }
                else
                {
                    var targetName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
                    var convertBackMethod = prop.ConvertBackMethod;
                    if (!string.IsNullOrEmpty(convertBackMethod))
                    {
                        sb.AppendLine("            " + targetName + " = " + prop.ConvertContainingType + "." + convertBackMethod + "(this." + prop.Name + ")" + comma);
                    }
                    else
                    {
                        sb.AppendLine("            " + targetName + " = this." + prop.Name + comma);
                    }
                }
            }

            // Emit reverse-flattened groups
            foreach (var kvp in validGroups)
            {
                itemIndex++;
                var navPropName = kvp.Key;
                var group = kvp.Value;
                var comma = itemIndex < totalItems ? "," : "";

                // Get navigation type from the first property in the group
                var navType = group[0].FlattenedNavigationType;
                if (string.IsNullOrEmpty(navType))
                {
                    // Cannot reverse-flatten without knowing the navigation type
                    continue;
                }

                sb.AppendLine("            " + navPropName + " = new " + navType);
                sb.AppendLine("            {");

                for (int j = 0; j < group.Count; j++)
                {
                    var flatProp = group[j];
                    var innerComma = j < group.Count - 1 ? "," : "";
                    // Extract the leaf property name from the flattened path
                    var dotIdx = flatProp.FlattenedPath.IndexOf('.');
                    var leafPath = flatProp.FlattenedPath.Substring(dotIdx + 1);

                    var convertBack = flatProp.ConvertBackMethod;
                    if (!string.IsNullOrEmpty(convertBack))
                    {
                        sb.AppendLine("                " + leafPath + " = " + flatProp.ConvertContainingType + "." + convertBack + "(this." + flatProp.Name + ")" + innerComma);
                    }
                    else
                    {
                        sb.AppendLine("                " + leafPath + " = this." + flatProp.Name + innerComma);
                    }
                }

                sb.AppendLine("            }" + comma);
            }

            sb.AppendLine("        };");
            sb.AppendLine("        OnAfterToSource(result);");
            sb.AppendLine("        return result;");
            sb.AppendLine("    }");

            return sb.ToString().TrimEnd();
        }
    }
}
