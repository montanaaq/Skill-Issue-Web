using System.Security.Cryptography;
using SkillIssue.Modules.Users.Models;
using SkillIssue.Modules.Users.Repositories;

namespace SkillIssue.Modules.Users.Services;

public interface IAuthService
{
    Task<int?> RegisterAsync(
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken
    );

    Task<User?> GetCurrentUserAsync(Session? session, CancellationToken cancellationToken);

    Task<SignInResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken
    );

    Task<(Session? Session, bool Renewed)> FindSessionAsync(
        string token,
        CancellationToken cancellationToken
    );

    Task SignOutAsync(string? token, CancellationToken cancellationToken);
}

public sealed class AuthService(
    IUserRepository userRepository,
    ISessionRepository sessionRepository
) : IAuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan SessionRenewalThreshold = TimeSpan.FromHours(12);
    private static readonly TimeSpan MaximumSessionLifetime = TimeSpan.FromDays(7);
    private const int TokenSizeInBytes = 32;
    private const int TokenLength = TokenSizeInBytes * 2;

    public async Task<int?> RegisterAsync(
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken
    )
    {
        string passwordHash = PasswordHasher.Hash(password);

        return await userRepository.CreateAsync(
            email.Trim(),
            fullName.Trim(),
            passwordHash,
            cancellationToken
        );
    }

    public async Task<User?> GetCurrentUserAsync(
        Session? session,
        CancellationToken cancellationToken
    )
    {
        if (session is null)
        {
            return null;
        }

        return await userRepository.FindByIdAsync(session.UserId, cancellationToken);
    }

    public async Task<SignInResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken
    )
    {
        var user = await userRepository.FindByEmailAsync(email.Trim(), cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        byte[] tokenBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        string token = Convert.ToHexStringLower(tokenBytes);
        byte[] tokenHash = SHA256.HashData(tokenBytes);
        var expiresAt = DateTime.UtcNow.Add(SessionLifetime);

        await sessionRepository.CreateAsync(user.Id, tokenHash, expiresAt, cancellationToken);

        return new SignInResult(user, token, expiresAt);
    }

    public async Task<(Session? Session, bool Renewed)> FindSessionAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        bool renewed = false;

        byte[]? tokenHash = HashToken(token);

        if (tokenHash is null)
        {
            return (null, renewed);
        }

        var session = await sessionRepository.FindByTokenHashAsync(tokenHash, cancellationToken);
        var now = DateTime.UtcNow;

        if (
            session is null
            || session.RevokedAt is not null
            || session.ExpiresAt <= now
            || session.CreatedAt.Add(MaximumSessionLifetime) <= now
        )
        {
            return (null, renewed);
        }

        var maximumExpiresAt = session.CreatedAt.Add(MaximumSessionLifetime);
        var expiresAt = now.Add(SessionLifetime);

        if (expiresAt > maximumExpiresAt)
        {
            expiresAt = maximumExpiresAt;
        }

        if (session.ExpiresAt - now > SessionRenewalThreshold || expiresAt <= session.ExpiresAt)
        {
            return (session, renewed);
        }

        var renewedSession = await sessionRepository.RenewAsync(
            session.Id,
            expiresAt,
            cancellationToken
        );

        renewed = renewedSession is not null;

        return (renewedSession, renewed);
    }

    public async Task SignOutAsync(string? token, CancellationToken cancellationToken)
    {
        byte[]? tokenHash = HashToken(token);

        if (tokenHash is not null)
        {
            await sessionRepository.RevokeAsync(tokenHash, cancellationToken);
        }
    }

    private static byte[]? HashToken(string? token)
    {
        if (token is null || token.Length != TokenLength)
        {
            return null;
        }

        byte[] tokenBytes;

        try
        {
            tokenBytes = Convert.FromHexString(token);
        }
        catch (FormatException)
        {
            return null;
        }

        return SHA256.HashData(tokenBytes);
    }
}
