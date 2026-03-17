namespace Facette.Abstractions;

/// <summary>
/// Predefined configuration presets for common CRUD mapping scenarios.
/// </summary>
public enum FacettePreset
{
    /// <summary>
    /// No preset applied. All options use their individual default values.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Preset for create/update DTOs. Disables <c>Projection</c> generation
    /// and enables <c>ToSource()</c> for reverse mapping.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Preset for read-only DTOs. Enables <c>Projection</c> generation
    /// and disables <c>ToSource()</c> since read DTOs are not mapped back.
    /// </summary>
    Read = 2
}
