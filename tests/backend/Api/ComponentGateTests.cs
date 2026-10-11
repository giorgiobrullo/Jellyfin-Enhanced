using Jellyfin.Plugin.JellyfinEnhanced.Services;

namespace JE.Tests;

public class ComponentGateTests
{
    [Theory]
    [InlineData("A", true)]
    [InlineData("B", false)]
    [InlineData("A && B", false)]
    [InlineData("A || B", true)]
    [InlineData("!B", true)]
    [InlineData("A && (B || C)", true)]
    [InlineData("B && (A || C)", false)]
    [InlineData("A && (", true)]
    [InlineData("", true)]
    public void GatesEvaluateOverFlags(string gate, bool expected)
    {
        bool Flag(string name) => name switch { "A" => true, "B" => false, "C" => true, _ => true };
        Assert.Equal(expected, GateExpression.Evaluate(gate, Flag));
    }

    [Fact]
    public void EveryGateNamesAManifestModule()
    {
        var manifest = ClientScriptBundle.GetComponentScripts().ToHashSet();
        Assert.NotEmpty(ClientScriptBundle.GetComponentGates());
        Assert.All(ClientScriptBundle.GetComponentGates().Keys, path => Assert.Contains(path, manifest));
    }

    [Fact]
    public void FeaturesOffLeaveTheirModulesOutAndChangeTheKey()
    {
        var all = ClientScriptBundle.GetComponentScripts();
        var everythingOn = ClientScriptBundle.GetComponentScripts(_ => true);
        var everythingOff = ClientScriptBundle.GetComponentScripts(_ => false);

        Assert.Equal(all, everythingOn);
        Assert.True(everythingOff.Count < all.Count);
        Assert.DoesNotContain(everythingOff, path => ClientScriptBundle.GetComponentGates().ContainsKey(path));
        Assert.Equal("v1", ClientScriptBundle.GetBundleKey("v1", everythingOn));
        Assert.StartsWith("v1-", ClientScriptBundle.GetBundleKey("v1", everythingOff));

        // The order of the manifest is kept: modules run in it.
        Assert.Equal(all.Where(everythingOff.Contains), everythingOff);
    }

    [Fact]
    public void ATrimmedBundleCountsOnlyItsModules()
    {
        using var fixture = new CoreFixture();
        var trimmed = ClientScriptBundle.GetComponentScripts(_ => false);
        var key = ClientScriptBundle.GetBundleKey("gate-test", trimmed);
        var (script, _) = ClientScriptBundle.GetBundle(key, trimmed, rebuild: true, fixture.Logger);
        Assert.Contains($"window.__JE_BUNDLE_TOTAL = {trimmed.Count};", System.Text.Encoding.UTF8.GetString(script));
    }
}
