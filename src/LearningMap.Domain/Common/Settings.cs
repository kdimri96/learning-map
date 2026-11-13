namespace LearningMap.Domain.Common;

// "Options pattern": each class below is bound to a section of appsettings.json
// via services.Configure<T>(configuration.GetSection(T.SectionName)).

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>HMAC-SHA256 key. At least 32 characters. Keep it in user-secrets, never in appsettings.json.</summary>
    public string IssuerSigningKey { get; set; } = string.Empty;
    public string ValidIssuer { get; set; } = string.Empty;
    public string ValidAudience { get; set; } = string.Empty;
    public int TokenExpirationInMinutes { get; set; } = 60;
}

public class BlobStorageSettings
{
    public const string SectionName = "BlobStorageSettings";

    public string ContainerName { get; set; } = "employee-images";
    /// <summary>How long a generated image read-URL (SAS) stays valid.</summary>
    public int SasExpiryMinutes { get; set; } = 60;
}

public class SeedAdminSettings
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>Kept in user-secrets. If empty, no admin is seeded.</summary>
    public string Password { get; set; } = string.Empty;
}
