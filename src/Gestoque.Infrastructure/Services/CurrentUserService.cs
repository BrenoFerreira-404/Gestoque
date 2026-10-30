using Gestoque.Application.Common.Interfaces;
using Gestoque.Domain.Enums;

namespace Gestoque.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }
    public UserRole? Role { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;

    public void SetUser(Guid userId, string email, string displayName, UserRole role)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        Role = role;
    }

    public void Clear()
    {
        UserId = null;
        Email = null;
        DisplayName = null;
        Role = null;
    }
}
