using Facette.Tests.Helpers;
using Xunit;

namespace Facette.Tests;

public class MultiSourceTests
{
    [Fact]
    public void MultiSource_GeneratesFromSourceWithMultipleParams()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UserProfile
{
    public string Bio { get; set; }
    public string AvatarUrl { get; set; }
}

[Facette(typeof(User))]
[AdditionalSource(typeof(UserProfile))]
public partial record UserDetailDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("UserDetailDto.g.cs"))
            .GetText().ToString();

        // FromSource should have two parameters
        Assert.Contains("FromSource(global::TestNs.User source, global::TestNs.UserProfile userProfile)", generated);

        // Should map primary source properties
        Assert.Contains("Id = source.Id", generated);
        Assert.Contains("Name = source.Name", generated);

        // Should map additional source properties
        Assert.Contains("Bio = userProfile.Bio", generated);
        Assert.Contains("AvatarUrl = userProfile.AvatarUrl", generated);
    }

    [Fact]
    public void MultiSource_WithPrefix_PrefixesPropertyNames()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UserSettings
{
    public string Theme { get; set; }
    public bool DarkMode { get; set; }
}

[Facette(typeof(User))]
[AdditionalSource(typeof(UserSettings), ""Settings"")]
public partial record UserWithSettingsDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("UserWithSettingsDto.g.cs"))
            .GetText().ToString();

        // Properties from additional source should have prefix
        Assert.Contains("SettingsTheme { get; init; }", generated);
        Assert.Contains("SettingsDarkMode { get; init; }", generated);

        // Mapped from the settings parameter
        Assert.Contains("SettingsTheme = settings.Theme", generated);
        Assert.Contains("SettingsDarkMode = settings.DarkMode", generated);
    }

    [Fact]
    public void MultiSource_ToSource_SkipsAdditionalSourceProperties()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class UserProfile
{
    public string Bio { get; set; }
}

[Facette(typeof(User))]
[AdditionalSource(typeof(UserProfile))]
public partial record UserDetailDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("UserDetailDto.g.cs"))
            .GetText().ToString();

        // ToSource should only map primary source properties
        var toSourceIdx = generated.IndexOf("ToSource()");
        Assert.True(toSourceIdx >= 0);
        var toSourceSection = generated.Substring(toSourceIdx);
        var nextMethod = toSourceSection.IndexOf("Projection");
        if (nextMethod > 0) toSourceSection = toSourceSection.Substring(0, nextMethod);

        Assert.Contains("Id = this.Id", toSourceSection);
        Assert.Contains("Name = this.Name", toSourceSection);
        Assert.DoesNotContain("Bio", toSourceSection);
    }

    [Fact]
    public void MultiSource_HookIncludesAdditionalParams()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class User
{
    public int Id { get; set; }
}

public class Extra
{
    public string Tag { get; set; }
}

[Facette(typeof(User))]
[AdditionalSource(typeof(Extra))]
public partial record UserPlusDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("UserPlusDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("partial void OnAfterFromSource(global::TestNs.User source, global::TestNs.Extra extra)", generated);
    }
}
