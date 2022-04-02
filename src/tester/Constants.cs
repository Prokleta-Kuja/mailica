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
}