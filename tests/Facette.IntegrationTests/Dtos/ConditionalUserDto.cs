using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(User), NestedDtos = new[] { typeof(AddressDto) })]
public partial record ConditionalUserDto
{
    private static bool _shouldMapEmail = true;

    [MapWhen(nameof(ShouldMapEmail))]
    public string Email { get; init; }

    public static bool ShouldMapEmail() => _shouldMapEmail;

    public static void SetShouldMapEmail(bool value) => _shouldMapEmail = value;
}
