namespace LearningMap.Application.Dtos;

// DTO = Data Transfer Object: the shape we SEND to clients.
// We never return entities directly (that would leak PasswordHash, internal ids, IsDeleted...).

public record LoginResponseDto(string Token, DateTimeOffset ExpiresAt, Guid UserGuid, string FullName, string Email, string Role);

public record UserSummaryDto(Guid UserGuid, string FullName, string Email, string Role);

public class EmployeeDto
{
    public Guid EmployeeGuid { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }
    /// <summary>Temporary SAS URL, valid for BlobStorageSettings.SasExpiryMinutes.</summary>
    public string? ImageUrl { get; set; }
    public bool HasImage { get; set; }
    public DateTimeOffset? ImageUploadedDate { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? ModifiedDate { get; set; }
}

/// <summary>Filled by Dapper from the columns returned by dbo.usp_GetDepartmentSummary.</summary>
public class DepartmentSummaryDto
{
    public string Department { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public DateTime? LatestJoiningDate { get; set; }
}

public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
