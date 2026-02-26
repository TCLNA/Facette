#pragma warning disable CS8604 // Null passed to Equals(T) overrides — null is handled inside each method
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Facette.Generator.Models
{
    public sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
    {
        public DiagnosticInfo(DiagnosticDescriptor descriptor, string filePath, Microsoft.CodeAnalysis.Text.TextSpan textSpan, Microsoft.CodeAnalysis.Text.LinePositionSpan lineSpan, object[] messageArgs)
        {
            Descriptor = descriptor;
            FilePath = filePath;
            TextSpan = textSpan;
            LineSpan = lineSpan;
            MessageArgs = messageArgs;
        }

        public DiagnosticDescriptor Descriptor { get; }
        public string FilePath { get; }
        public Microsoft.CodeAnalysis.Text.TextSpan TextSpan { get; }
        public Microsoft.CodeAnalysis.Text.LinePositionSpan LineSpan { get; }
        public object[] MessageArgs { get; }

        public Location ToLocation()
        {
            return Location.Create(FilePath, TextSpan, LineSpan);
        }

        public bool Equals(DiagnosticInfo other)
        {
            if (other == null) return false;
            return Descriptor.Id == other.Descriptor.Id
                && FilePath == other.FilePath
                && TextSpan.Equals(other.TextSpan);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DiagnosticInfo);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (Descriptor.Id != null ? Descriptor.Id.GetHashCode() : 0);
                hash = hash * 31 + (FilePath != null ? FilePath.GetHashCode() : 0);
                hash = hash * 31 + TextSpan.GetHashCode();
                return hash;
            }
        }
    }

    public sealed class FacetteTargetModel : IEquatable<FacetteTargetModel>
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

        public bool Equals(FacetteTargetModel other)
        {
            if (other == null) return false;
            if (Namespace != other.Namespace) return false;
            if (TypeName != other.TypeName) return false;
            if (SourceTypeFullName != other.SourceTypeFullName) return false;
            if (GenerateToSource != other.GenerateToSource) return false;
            if (GenerateProjection != other.GenerateProjection) return false;
            if (GenerateMapper != other.GenerateMapper) return false;
            if (Properties.Length != other.Properties.Length) return false;

            for (int i = 0; i < Properties.Length; i++)
            {
                if (!Properties[i].Equals(other.Properties[i])) return false;
            }

            if (Diagnostics.Length != other.Diagnostics.Length) return false;
            for (int i = 0; i < Diagnostics.Length; i++)
            {
                if (!Diagnostics[i].Equals(other.Diagnostics[i])) return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FacetteTargetModel);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (Namespace != null ? Namespace.GetHashCode() : 0);
                hash = hash * 31 + (TypeName != null ? TypeName.GetHashCode() : 0);
                hash = hash * 31 + (SourceTypeFullName != null ? SourceTypeFullName.GetHashCode() : 0);
                hash = hash * 31 + GenerateToSource.GetHashCode();
                hash = hash * 31 + GenerateProjection.GetHashCode();
                hash = hash * 31 + GenerateMapper.GetHashCode();
                hash = hash * 31 + Properties.Length.GetHashCode();
                return hash;
            }
        }
    }

    public enum MappingKind
    {
        Direct,
        Custom,
        Nested,
        Collection
    }

    public sealed class PropertyModel : IEquatable<PropertyModel>
    {
        public PropertyModel(
            string name,
            string typeFullName,
            bool isValueType,
            MappingKind mappingKind,
            string sourcePropertyName,
            string nestedDtoTypeName,
            string nestedDtoTypeFullName,
            string collectionElementTypeFullName,
            bool isNullable,
            bool isArray)
        {
            Name = name;
            TypeFullName = typeFullName;
            IsValueType = isValueType;
            MappingKind = mappingKind;
            SourcePropertyName = sourcePropertyName;
            NestedDtoTypeName = nestedDtoTypeName;
            NestedDtoTypeFullName = nestedDtoTypeFullName;
            CollectionElementTypeFullName = collectionElementTypeFullName;
            IsNullable = isNullable;
            IsArray = isArray;
        }

        public string Name { get; }
        public string TypeFullName { get; }
        public bool IsValueType { get; }
        public MappingKind MappingKind { get; }
        public string SourcePropertyName { get; }
        public string NestedDtoTypeName { get; }
        public string NestedDtoTypeFullName { get; }
        public string CollectionElementTypeFullName { get; }
        public bool IsNullable { get; }
        public bool IsArray { get; }

        public static PropertyModel Direct(string name, string typeFullName, bool isValueType)
        {
            return new PropertyModel(
                name, typeFullName, isValueType,
                MappingKind.Direct, name, "", "", "", false, false);
        }

        public bool Equals(PropertyModel other)
        {
            if (other == null) return false;
            return Name == other.Name
                && TypeFullName == other.TypeFullName
                && IsValueType == other.IsValueType
                && MappingKind == other.MappingKind
                && SourcePropertyName == other.SourcePropertyName
                && NestedDtoTypeName == other.NestedDtoTypeName
                && NestedDtoTypeFullName == other.NestedDtoTypeFullName
                && CollectionElementTypeFullName == other.CollectionElementTypeFullName
                && IsNullable == other.IsNullable
                && IsArray == other.IsArray;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PropertyModel);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (Name != null ? Name.GetHashCode() : 0);
                hash = hash * 31 + (TypeFullName != null ? TypeFullName.GetHashCode() : 0);
                hash = hash * 31 + IsValueType.GetHashCode();
                hash = hash * 31 + (int)MappingKind;
                hash = hash * 31 + (SourcePropertyName != null ? SourcePropertyName.GetHashCode() : 0);
                hash = hash * 31 + (NestedDtoTypeName != null ? NestedDtoTypeName.GetHashCode() : 0);
                hash = hash * 31 + (NestedDtoTypeFullName != null ? NestedDtoTypeFullName.GetHashCode() : 0);
                hash = hash * 31 + (CollectionElementTypeFullName != null ? CollectionElementTypeFullName.GetHashCode() : 0);
                hash = hash * 31 + IsNullable.GetHashCode();
                hash = hash * 31 + IsArray.GetHashCode();
                return hash;
            }
        }
    }
}
