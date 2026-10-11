namespace StockHub.Api.Contracts;

public sealed record OnboardingTask(
    string Key,
    string Title,
    string Status,
    string? Handoff)
{
    public static IReadOnlyList<OnboardingTask> CreateChecklist(string? selectedActionKey)
    {
        bool IsSelected(string actionKey) => string.Equals(
            actionKey,
            selectedActionKey,
            StringComparison.Ordinal);

        return
        [
            new("import-products", "Import products", IsSelected("import-products") ? "selected" : "available", "inventory"),
            new("connect-platform", "Connect a platform", IsSelected("connect-platform") ? "selected" : "available", "platforms"),
            new("invite-team", "Invite your team", IsSelected("invite-team") ? "selected" : "available", "team")
        ];
    }

    public static bool IsSupportedAction(string? actionKey) => actionKey is
        "import-products" or "connect-platform" or "invite-team";
}
