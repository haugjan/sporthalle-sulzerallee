using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace SporthalleWeb.Tests.Architecture;

/// <summary>
/// Architecture guards for the vertical-slice layering (see CLAUDE.md, "Architecture:
/// Vertical Slicing"), enforced with ArchUnitNET against the compiled assembly. Unlike a
/// source scan, this checks real type dependencies (including those introduced by .razor
/// components, which compile into the same assembly).
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(BookingSlot).Assembly)
        .Build();

    // Anchored regexes: match the namespace itself and any sub-namespace, nothing else.
    private const string DomainNs        = @"^SporthalleWeb\.Domain($|\.)";
    private const string FeaturesNs      = @"^SporthalleWeb\.Features($|\.)";
    private const string InfrastructureNs = @"^SporthalleWeb\.Infrastructure($|\.)";
    private const string BookingDomainNs = @"^SporthalleWeb\.Domain\.Booking($|\.)";
    private const string PassivDomainNs  = @"^SporthalleWeb\.Domain\.PassiveMembership($|\.)";
    private const string FrameworksNs    = @"^(Umbraco|NPoco)($|\.)";

    [Fact]
    public void Domain_should_not_depend_on_inner_outer_layers()
    {
        Types().That().ResideInNamespaceMatching(DomainNs)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(
                    $"{FeaturesNs}|{InfrastructureNs}"))
            .Because("the Domain layer must not reference application or infrastructure layers.")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_should_not_depend_on_frameworks()
    {
        Types().That().ResideInNamespaceMatching(DomainNs)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(FrameworksNs))
            .Because("the Domain layer must stay free of Umbraco/NPoco and other frameworks.")
            .Check(Architecture);
    }

    [Fact]
    public void Features_should_not_depend_on_Infrastructure()
    {
        Types().That().ResideInNamespaceMatching(FeaturesNs)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(InfrastructureNs))
            .Because("Features (application layer) must talk to a Port, not an Infrastructure adapter.")
            .Check(Architecture);
    }

    [Fact]
    public void Booking_domain_should_not_depend_on_PassiveMembership_domain()
    {
        Types().That().ResideInNamespaceMatching(BookingDomainNs)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(PassivDomainNs))
            .Because("the Booking and PassiveMembership bounded contexts must stay isolated.")
            .Check(Architecture);
    }

    [Fact]
    public void PassiveMembership_domain_should_not_depend_on_Booking_domain()
    {
        Types().That().ResideInNamespaceMatching(PassivDomainNs)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(BookingDomainNs))
            .Because("the Booking and PassiveMembership bounded contexts must stay isolated.")
            .Check(Architecture);
    }
}
