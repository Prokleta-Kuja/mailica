using System.Text;

namespace mailica.Services;

public static class DovecotConfiguration
{
    const string MAIN = "dovecot.conf";
    const string SQL_USER = "sql-user.conf";
    const string SQL_PASS = "sql-pass.conf";
    const string SQL_MASTER = "sql-master.conf";
    const string SYSTEM = "system.conf";
    static void GenerateSqlUser()
    {
        File.WriteAllText(C.Paths.ConfigDataFor(SQL_USER), @"
driver = sqlite
connect = /etc/dovecot/app.db
user_query = SELECT '*:bytes=' || Quota AS quota_rule FROM users WHERE Name = '%Ln' AND Disabled IS NULL
iterate_query = SELECT Name AS username FROM Users WHERE Disabled IS NULL");
    }
    static void GenerateSqlPass()
    {
        File.WriteAllText(C.Paths.ConfigDataFor(SQL_PASS), @"
driver = sqlite
connect = /etc/dovecot/app.db
password_query = SELECT Name AS username, Password AS password, '*:bytes=' || Quota AS quota_rule \
FROM Users WHERE Name = '%Ln' AND Disabled IS NULL AND IsMaster = 0");
    }
    static void GenerateSqlMaster()
    {
        File.WriteAllText(C.Paths.ConfigDataFor(SQL_MASTER), @"
driver = sqlite
connect = /etc/dovecot/app.db
password_query = SELECT Name AS username, Password AS password, '*:bytes=' || Quota AS quota_rule \
FROM Users WHERE Name = '%Ln' AND Disabled IS NULL AND IsMaster = 1");
    }
    static void GenerateSystem()
    {
        var pair = DovecotHasher.Hash(C.MasterSecret);
        var pass = DovecotHasher.Password(pair.Salt, pair.Hash);
        File.WriteAllText(C.Paths.ConfigDataFor(SYSTEM), @$"
passdb {{
  driver = static
  args = user={C.MasterUser} password={pass}
  master = yes
  result_success = continue-ok
}}");
    }
    static void GenerateMain()
    {
        var main = new StringBuilder();
        if (C.IsDebug)
            main.AppendLine("auth_debug = yes");

        main.AppendLine(@$"
ssl=required
ssl_cert = </certs/{C.CRT_FILE}
ssl_key = </certs/{C.KEY_FILE}
protocols = imap
mail_home = /mail/%Ln
mail_location = maildir:/mail/%Ln
auth_master_user_separator=*
mail_plugins = $mail_plugins quota
mailbox_list_index = yes
mail_always_cache_fields = date.save
mailbox_list_index = yes
protocol imap {{
  mail_plugins = $mail_plugins imap_quota
}}
protocol !indexer-worker {{
  mail_vsize_bg_after_count = 100
}}
plugin {{
  quota_exceeded_message = Quota exceeded, please go to delete some messages first.
  quota = count:User quota
  quota_vsizes = yes
  #quota_rule = *:storage=1G # This will be the default overrriden by SQL.
}}

namespace {{
  inbox = yes
  separator = /

  mailbox Drafts {{
    special_use = \Drafts
    auto = subscribe
  }}

  mailbox Junk {{
    special_use = \Junk
    auto = create
    autoexpunge = 14d
  }}

  mailbox Spam {{
    special_use = \Junk
    auto = no
  }}

  mailbox Trash {{
    special_use = \Trash
    auto = subscribe
    autoexpunge = 14d
  }}

  mailbox Sent {{
    special_use = \Sent
    auto = subscribe
  }}

  mailbox ""Sent Mail"" {{
    special_use = \Sent
    auto = no
  }}

  mailbox Archive {{
        special_use = \Archive
        auto = create
  }}
}}

log_path=/dev/stdout
info_log_path=/dev/stdout
debug_log_path=/dev/stdout");

        // TODO: insert proper uid gid
        main.AppendLine($@"
mail_uid = 1000
mail_gid = 1000");

        // Permissions
        main.AppendLine($@"
!include {SYSTEM}
passdb {{
  driver = sql
  args = /etc/dovecot/{SQL_MASTER}
  master = yes
  result_success = continue-ok
}}
passdb {{
  driver = sql
  args = /etc/dovecot/{SQL_PASS}
}}
userdb {{
  override_fields = uid=vmail gid=vmail
  driver = sql
  args = /etc/dovecot/{SQL_USER}
}}");

        File.WriteAllText(C.Paths.ConfigDataFor(MAIN), main.ToString());
    }
    public static void Initial()
    {
        GenerateSqlUser();
        GenerateSqlPass();
        GenerateSqlMaster();
        GenerateSystem();
        GenerateMain();
    }
}