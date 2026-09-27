using System.Net.Mail;

namespace StockHub.Api;

public static class AccessValidation
{
    public static Problem? SignUp(SignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return new("Validation failed", "Full name is required.", "full_name_required");
        if (!IsEmail(request.Email)) return new("Validation failed", "Enter a valid email address.", "email_invalid");
        if (request.Password.Length < 8) return new("Validation failed", "Password must be at least 8 characters.", "password_too_short");
        return null;
    }

    public static Problem? SignIn(SignInRequest request) => IsEmail(request.Email) && !string.IsNullOrEmpty(request.Password) ? null : new("Validation failed", "Email and password are required.", "credentials_invalid");

    public static Problem? Workspace(CreateWorkspaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName)) return new("Validation failed", "Business name is required.", "business_name_required");
        if (request.Country.Length != 2) return new("Validation failed", "Country must be an ISO 3166-1 alpha-2 code.", "country_invalid");
        if (request.Currency.Length != 3) return new("Validation failed", "Currency must be an ISO 4217 code.", "currency_invalid");
        return null;
    }

    private static bool IsEmail(string value) => !string.IsNullOrWhiteSpace(value) && MailAddress.TryCreate(value.Trim(), out var address) && address.Address == value.Trim();
}
