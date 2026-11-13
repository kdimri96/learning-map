namespace LearningMap.Domain.Entities;

/// <summary>A person who can log in to the API.</summary>
public class AppUser : IAuditableEntity
{
    /// <summary>Internal primary key (identity column). Never exposed outside the API.</summary>
    public int UserId { get; set; }

    /// <summary>Public identifier, safe to show to clients (same idea as SafeSpace's UserGuid).</summary>
    public Guid UserGuid { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>Always stored lower-case so lookups are case-insensitive.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Argon2id hash with the salt prepended, Base64-encoded. The plain password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = Common.AppRoles.Viewer;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginDate { get; set; }

    public int? CreatedBy { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedDate { get; set; }
}
