using Gestoque.Application.Common.Auth;
using Gestoque.Application.Common.Interfaces;
using Gestoque.Application.DTOs;
using Gestoque.Domain.Entities;
using Gestoque.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gestoque.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResultDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
{
    private readonly IGestoqueDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;

    public LoginCommandHandler(IGestoqueDbContext context, IPasswordHasherService passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        string email = NormalizeEmail(request.Email);
        User? user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResultDto(user.Id, user.Email, user.DisplayName, user.Role, user.TenantId);
    }

    internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public record GetCurrentUserQuery : IRequest<UserDto?>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IGestoqueDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return null;

        User? user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user is null)
            return null;

        return Map(user);
    }

    internal static UserDto Map(User user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.TenantId,
            user.Tenant?.NomeFantasia,
            user.IsActive,
            user.LastLoginAt);
}

public record CreateUserCommand(
    string Email,
    string Password,
    string DisplayName,
    UserRole Role,
    Guid? TenantId
) : IRequest<Guid>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IGestoqueDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCommandHandler(
        IGestoqueDbContext context,
        IPasswordHasherService passwordHasher,
        ICurrentUserService currentUser)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        AuthGuard.RequireSuperAdmin(_currentUser);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ArgumentException("A senha deve ter pelo menos 8 caracteres.");

        if (request.Role == UserRole.Operator && !request.TenantId.HasValue)
            throw new ArgumentException("Usuário operacional precisa estar ligado a uma empresa.");

        if (request.Role == UserRole.SuperAdmin && request.TenantId.HasValue)
            throw new ArgumentException("Administrador geral não deve ter empresa fixa.");

        string email = LoginCommandHandler.NormalizeEmail(request.Email);
        bool exists = await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Já existe um usuário com este e-mail.");

        if (request.TenantId.HasValue)
        {
            bool tenantExists = await _context.Tenants.AnyAsync(t => t.Id == request.TenantId.Value, cancellationToken);
            if (!tenantExists)
                throw new KeyNotFoundException("Empresa não encontrada.");
        }

        User user = new()
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role,
            TenantId = request.Role == UserRole.SuperAdmin ? null : request.TenantId,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}

public record ListUsersQuery : IRequest<List<UserDto>>;

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, List<UserDto>>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ListUsersQueryHandler(IGestoqueDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        AuthGuard.RequireSuperAdmin(_currentUser);

        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Tenant)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserDto(
                u.Id,
                u.Email,
                u.DisplayName,
                u.Role,
                u.TenantId,
                u.Tenant != null ? u.Tenant.NomeFantasia : null,
                u.IsActive,
                u.LastLoginAt))
            .ToListAsync(cancellationToken);
    }
}

public record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest;

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IGestoqueDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SetUserActiveCommandHandler(IGestoqueDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        AuthGuard.RequireSuperAdmin(_currentUser);

        User user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        if (!request.IsActive && user.Role == UserRole.SuperAdmin)
        {
            int activeAdmins = await _context.Users.CountAsync(
                u => u.Role == UserRole.SuperAdmin && u.IsActive && u.Id != user.Id,
                cancellationToken);
            if (activeAdmins == 0)
                throw new InvalidOperationException("Não é possível inativar o último administrador geral.");
        }

        user.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record ChangePasswordCommand(Guid UserId, string NewPassword) : IRequest;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IGestoqueDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(
        IGestoqueDbContext context,
        IPasswordHasherService passwordHasher,
        ICurrentUserService currentUser)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        AuthGuard.RequireAuthenticated(_currentUser);

        bool isSelf = _currentUser.UserId == request.UserId;
        if (!isSelf && !_currentUser.IsSuperAdmin)
            throw new ForbiddenException("Você só pode alterar a própria senha.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ArgumentException("A senha deve ter pelo menos 8 caracteres.");

        User user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
