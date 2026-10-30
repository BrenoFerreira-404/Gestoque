using Gestoque.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Gestoque.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(new object(), password);
    }

    public bool Verify(string hash, string password)
    {
        PasswordVerificationResult result = _hasher.VerifyHashedPassword(new object(), hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
