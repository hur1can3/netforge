namespace NetForge.Core.Options;

// TODO(core-options-001): Expand with additional toggles for scanning filters, exception wrapping, logging integration.
public sealed class ForgeOptions
{
    public bool EnableAssemblyScanning { get; set; } = true; // TODO(core-options-002): Use in DI extension.
    public bool ThrowOnUnhandledRequest { get; set; } = true; // TODO(core-options-003): Enforce in mediator when false (return default?)
    public bool RegisterDefaultBehaviors { get; set; } = true; // TODO(core-options-004): Conditional registration of default behaviors.
}
