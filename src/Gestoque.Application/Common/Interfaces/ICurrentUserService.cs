using Gestoque.Domain.Enums;

namespace Gestoque.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }

    void SetUser(Guid userId, string email, string displayName, UserRole role);
    void Clear();
}
