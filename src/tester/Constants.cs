using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MimeKit;

namespace tester
{
    public static class C
    {
        public const string Lipsum = @"Est ea neque dolorem atque suscipit sunt. Eum quos omnis eius. Doloremque molestias porro dolores quibusdam est aut adipisci doloremque.

Nam nihil officia repudiandae dolor ea esse perspiciatis. Voluptatem aut dicta optio ipsum sed ab possimus. Et molestiae aut consectetur necessitatibus occaecati itaque placeat consequatur. Id enim facere quia vero ea sed et. Molestias necessitatibus beatae porro sit assumenda dolores. Debitis aut nemo minima omnis odio tenetur.

Sit aperiam doloremque deserunt consequuntur provident ut quis velit. Pariatur ut aspernatur sint error. Esse quod itaque doloremque a qui repellat enim error. Est corporis eum ea quia nobis omnis. Aliquam rerum id repellat libero consequatur assumenda aut.

Aspernatur dolor itaque et incidunt veritatis neque. Deserunt fugit eos id quasi laborum et quia ducimus. Sunt aut ullam fugit sit inventore suscipit ut. Optio dicta quia atque et. Similique et ut consequatur quia accusamus sint perspiciatis. At vel sed corrupti veniam ut.

Voluptatibus aut sed nobis reprehenderit nulla magni. Libero fugit veniam sunt est optio. Aut doloremque deserunt quo consectetur. Distinctio rem ut veritatis sed placeat. Quae sint molestiae autem nam aut.";
        public static readonly MailboxAddress BoxSender = new("External Sender", "external@te.st");
        public static readonly MailboxAddress BoxUser1 = new("User 1", "user1@te.st");
        public static readonly MailboxAddress BoxUser2 = new("User 2", "user2@te.st");
        public static readonly MailboxAddress BoxSales = new("Company Sales", "sales@te.st");
        public static readonly MailboxAddress BoxSupport = new("Company Support", "support@te.st");
        public static readonly MailboxAddress BoxGmailUser1 = new("Gmail User 1", "gmail-user1@te.st");
        public static readonly MailboxAddress BoxGmailCompany = new("Gmail Company", "gmail-company@te.st");
    }
    public static class PasswordHasher
    {
        const int KEY_LENGTH = 20;
        const int SALT_LENGTH = 16;
        const int ITERATIONS = 5000;
        static readonly byte[] s_saltChars = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(c => Convert.ToByte(c)).ToArray();
        public static bool Verify(string salt, string hash, string password)
        {
            var saltBytes = salt.Select(c => Convert.ToByte(c)).ToArray();
            var keyBytes = KeyDerivation.Pbkdf2(password, saltBytes, KeyDerivationPrf.HMACSHA1, ITERATIONS, KEY_LENGTH);

            return hash == Convert.ToHexString(keyBytes).ToLower();
        }
        public static (string salt, string hash) Hash(string password)
        {
            var salt = new byte[SALT_LENGTH];
            for (int i = 0; i < SALT_LENGTH; i++)
                salt[i] = s_saltChars[RandomNumberGenerator.GetInt32(SALT_LENGTH)];
            var key = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA1, ITERATIONS, KEY_LENGTH);

            return (Encoding.UTF8.GetString(salt), Convert.ToHexString(key).ToLower());
        }
        public static string DovecotHash(string salt, string hash) => $"{{PBKDF2}}$1${salt}${ITERATIONS}${hash}";
    }
}