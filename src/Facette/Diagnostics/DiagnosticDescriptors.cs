using Microsoft.CodeAnalysis;

namespace Facette.Generator.Diagnostics
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor FCT001_TypeMustBePartial = new DiagnosticDescriptor(
            id: "FCT001",
            title: "Type must be partial",
            messageFormat: "Type '{0}' must be declared as partial to use the [Facette] attribute",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT002_IncludeExcludeConflict = new DiagnosticDescriptor(
            id: "FCT002",
            title: "Include and Exclude cannot both be specified",
            messageFormat: "Type '{0}' specifies both Include and Exclude, which is not allowed",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT003_SourceTypeNotFound = new DiagnosticDescriptor(
            id: "FCT003",
            title: "Source type not found",
            messageFormat: "Source type specified in [Facette] on '{0}' could not be found or is inaccessible",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT004_PropertyNotFound = new DiagnosticDescriptor(
            id: "FCT004",
            title: "Property not found on source type",
            messageFormat: "Property '{0}' specified in Include/Exclude was not found on source type '{1}'",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT005_MapFromPropertyNotFound = new DiagnosticDescriptor(
            id: "FCT005",
            title: "MapFrom source property not found",
            messageFormat: "[MapFrom] on '{0}' references property '{1}' which was not found on source type '{2}'",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT006_CircularReference = new DiagnosticDescriptor(
            id: "FCT006",
            title: "Circular reference detected",
            messageFormat: "Circular reference detected in nested DTO chain involving source type '{0}'",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT007_FlattenedPathSegmentNotFound = new DiagnosticDescriptor(
            id: "FCT007",
            title: "Flattened path segment not found",
            messageFormat: "Path segment '{0}' in flattened path for property '{1}' was not found on type '{2}'",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT008_ConvertMethodNotFound = new DiagnosticDescriptor(
            id: "FCT008",
            title: "Convert method not found",
            messageFormat: "Convert method '{0}' specified on property '{1}' was not found as a static method on type '{2}'",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor FCT009_ConvertBackMethodNotFound = new DiagnosticDescriptor(
            id: "FCT009",
            title: "ConvertBack method not found",
            messageFormat: "ConvertBack method '{0}' specified on property '{1}' was not found as a static method on type '{2}'",
            category: "Facette",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
