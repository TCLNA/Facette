using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Facette.Generator.Models
{
    public sealed class DiagnosticInfo
    {
        public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, object[] messageArgs)
        {
            Descriptor = descriptor;
            Location = location;
            MessageArgs = messageArgs;
        }

        public DiagnosticDescriptor Descriptor { get; }
        public Location Location { get; }
        public object[] MessageArgs { get; }
    }

    public sealed class FacetteTargetModel
    {
        public FacetteTargetModel(
            string ns,
            string typeName,
            string sourceTypeFullName,
            ImmutableArray<PropertyModel> properties,
            bool generateToSource,
            bool generateProjection,
            bool generateMapper,
            ImmutableArray<DiagnosticInfo> diagnostics)
        {
            Namespace = ns;
            TypeName = typeName;
            SourceTypeFullName = sourceTypeFullName;
            Properties = properties;
            GenerateToSource = generateToSource;
            GenerateProjection = generateProjection;
            GenerateMapper = generateMapper;
            Diagnostics = diagnostics;
        }

        public string Namespace { get; }
        public string TypeName { get; }
        public string SourceTypeFullName { get; }
        public ImmutableArray<PropertyModel> Properties { get; }
        public bool GenerateToSource { get; }
        public bool GenerateProjection { get; }
        public bool GenerateMapper { get; }
        public ImmutableArray<DiagnosticInfo> Diagnostics { get; }
    }

    public sealed class PropertyModel
    {
        public PropertyModel(string name, string typeFullName, bool isValueType)
        {
            Name = name;
            TypeFullName = typeFullName;
            IsValueType = isValueType;
        }

        public string Name { get; }
        public string TypeFullName { get; }
        public bool IsValueType { get; }
    }
}
