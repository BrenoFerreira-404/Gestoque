using Gestoque.Application.Common.Interfaces;
using Gestoque.Domain.Enums;

namespace Gestoque.Application.Common.Auth;

public static class AuthGuard
{
    public static void RequireSuperAdmin(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || !currentUser.IsSuperAdmin)
            throw new ForbiddenException("Apenas o administrador geral pode executar esta ação.");
    }

    public static void RequireAuthenticated(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated)
            throw new ForbiddenException("É necessário estar autenticado.");
    }

    public static bool IsOperator(ICurrentUserService currentUser) =>
        currentUser.IsAuthenticated && currentUser.Role == UserRole.Operator;
}
