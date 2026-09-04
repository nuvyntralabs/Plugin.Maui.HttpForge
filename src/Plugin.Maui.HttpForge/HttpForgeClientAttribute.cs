using System.Diagnostics.CodeAnalysis;

namespace Plugin.Maui.HttpForge;

/// <summary>
/// Emitted by the source generator so the runtime can find the generated client
/// even when a <c>ModuleInitializer</c> is trimmed (common on Android).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class HttpForgeClientAttribute : Attribute
{
    public HttpForgeClientAttribute(
        Type contract,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementation)
    {
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        Implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
    }

    public Type Contract { get; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type Implementation { get; }
}
