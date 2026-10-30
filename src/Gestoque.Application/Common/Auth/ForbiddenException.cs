namespace Gestoque.Application.Common.Auth;

public class ForbiddenException : InvalidOperationException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
