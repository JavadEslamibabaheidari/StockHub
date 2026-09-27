namespace StockHub.Api.Tests;

public sealed class AccessValidationTests
{
    [Fact]
    public void SignUp_rejects_short_password_and_invalid_email()
    {
        var result = StockHub.Api.AccessValidation.SignUp(new("Ada", "not-an-email", "short"));

        Assert.NotNull(result);
        Assert.Equal("email_invalid", result!.Code);
    }

    [Fact]
    public void Workspace_requires_iso_country_and_currency_codes()
    {
        var result = StockHub.Api.AccessValidation.Workspace(new("Acme", "ITA", "EU", null));

        Assert.NotNull(result);
        Assert.Equal("country_invalid", result!.Code);
    }

    [Fact]
    public void Sign_in_rejects_missing_credentials_without_account_disclosure()
    {
        var result = StockHub.Api.AccessValidation.SignIn(new("", ""));

        Assert.Equal("credentials_invalid", result!.Code);
        Assert.DoesNotContain("account", result.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
