using System.Security.Cryptography;

namespace ExperimentSimulation.WebApi.Security
{
    public static class PasswordHasher
    {
        public static (string hashB64, string saltB64) HashPassword(string password, int iterations = 100_000, int saltBytes = 16, int keyBytes = 32)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(saltBytes);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(keyBytes);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public static bool Verify(string password, string saltB64, string expectedHashB64, int iterations = 100_000, int keyBytes = 32)
        {
            byte[] salt = Convert.FromBase64String(saltB64);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(keyBytes);

            string hashB64 = Convert.ToBase64String(hash);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hashB64),
                Convert.FromBase64String(expectedHashB64)
            );
        }
    }
}
