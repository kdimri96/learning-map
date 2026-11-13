namespace LearningMap.Domain.Entities;

/// <summary>
/// Any entity implementing this gets Created*/Modified* filled automatically
/// by AuditableEntityInterceptor (Infrastructure) when SaveChangesAsync runs.
/// </summary>
public interface IAuditableEntity
{
    int? CreatedBy { get; set; }
    DateTimeOffset CreatedDate { get; set; }
    int? ModifiedBy { get; set; }
    DateTimeOffset? ModifiedDate { get; set; }
}
