using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SystemLogin;

public class Authentication(AppDbContext db, PasswordHasher hasher)
{
    public async Task CreateUserAsync(string username, string password, bool isAdmin = false)
    {
        var (salt, saltedPasswordHash) = hasher.Hash(password);
        db.Users.Add(new User
        {
            Username = username,
            Salt = salt,
            SaltedPasswordHash = saltedPasswordHash,
            IsAdmin = isAdmin
        });
        await db.SaveChangesAsync();
    }

    public Task<bool> UsernameExistsAsync(string username)
    {
        return db.Users.AnyAsync(a => a.Username == username);
    }

    public async Task<bool> CredentialsCorrectAsync(string username, string password)
    {
        var account = await db.Users.FirstAsync(a => a.Username == username);
        return hasher.PasswordCorrect(password, account.Salt, account.SaltedPasswordHash);
    }

    public Task<bool> UserIsAdminAsync(string username)
    {
        return db.Users.Where(a => a.Username == username).Select(a => a.IsAdmin).FirstAsync();
    }

    public Task<User> GetUserAsync(string username)
    {
        return db.Users.FirstAsync(a => a.Username == username);
    }
}

public class PasswordHasher(
    int saltLength = 128 / 8,
    int hashIterations = 600_000
)
{
    public bool PasswordCorrect(string password, byte[] salt, byte[] saltedPasswordHash)
    {
        return CryptographicOperations.FixedTimeEquals(Hash(salt, password), saltedPasswordHash);
    }

    private byte[] Hash(byte[] salt, string password)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            hashIterations,
            HashAlgorithmName.SHA256,
            256 / 8
        );
    }

    public (byte[] Salt, byte[] Hash) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(saltLength);
        return (salt, Hash(salt, password));
    }
}
