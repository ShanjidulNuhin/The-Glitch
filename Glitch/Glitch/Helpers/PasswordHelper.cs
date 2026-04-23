using System.Security.Cryptography;
using System.Text;

namespace Glitch.Helpers
{
    public static class PasswordHelper
    {
        // Takes a plain text password and returns a hashed version
        // Example: "mypassword123" → "a665a45920422f..."
        public static string HashPassword(string password)
        {
            // Create a SHA256 hashing object
            using var sha256 = SHA256.Create();

            // Convert the password string into bytes
            // because SHA256 works on bytes not strings
            var bytes = Encoding.UTF8.GetBytes(password);

            // Perform the actual hashing
            var hash = sha256.ComputeHash(bytes);

            // Convert the hashed bytes back to a readable string
            // BitConverter makes it hex format like "a665a459..."
            // Replace("-", "") removes dashes between bytes
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        // Compares a plain text password with a stored hash
        // Returns true if they match, false if they don't
        public static bool VerifyPassword(string password, string storedHash)
        {
            // Hash the incoming plain text password
            var hashOfInput = HashPassword(password);

            // Compare it with the stored hash
            // StringComparer.OrdinalIgnoreCase ignores upper/lower case
            return StringComparer.OrdinalIgnoreCase.Compare(hashOfInput, storedHash) == 0;
        }
    }
}