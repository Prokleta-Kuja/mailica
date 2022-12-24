using System.Text;

namespace mailica.Services;

public static class DovecotConfiguration
{
    const string MAIN = "dovecot.conf";
    const string SQL_USER = "sql-user.conf";
    const string SQL_PASS = "sql-pass.conf";
    const string SQL_MASTER = "sql-master.conf";
    const string SYSTEM = "system.conf";
    public static void Initial()
    {
        if (Directory.GetFiles(C.Paths.ConfigData).Any())
            return;

        File.WriteAllText(C.Paths.ConfigDataFor(SQL_USER), @"
driver = sqlite
connect = /data/app.db
user_query = SELECT '*:bytes=' || Quota AS quota_rule FROM users WHERE Name = '%Ln'
iterate_query = SELECT Name AS username FROM Users");

        File.WriteAllText(C.Paths.ConfigDataFor(SQL_PASS), @"
driver = sqlite
connect = /data/app.db
password_query = SELECT Name AS username, Password AS password, '*:bytes=' || Quota AS quota_rule FROM Users WHERE Name = '%Ln' AND IsMaster = 0");

        File.WriteAllText(C.Paths.ConfigDataFor(SQL_MASTER), @"
driver = sqlite
connect = /data/app.db
password_query = SELECT Name AS username, Password AS password, '*:bytes=' ||  Quota AS quota_rule FROM Users WHERE Name = '%Ln' AND IsMaster = 1");

        var main = new StringBuilder();
        if (C.IsDebug)
            main.AppendLine("auth_debug = yes");

        main.AppendLine(@"
protocols = imap
mail_location = maildir:/srv/mail/%Ln
auth_master_user_separator=*
namespace {{
  inbox = yes
  separator = /
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
  args = {SQL_MASTER}
  master = yes
  result_success = continue-ok
}}
passdb {{
  driver = sql
  args = {SQL_PASS}
}}
userdb {{
  override_fields = uid=vmail gid=vmail
  driver = sql
  args = {SQL_USER}
}}");

        // TODO: reload certs https://doc.dovecot.org/admin_manual/doveadm_http_api/#doveadm-reload
        main.AppendLine($@"
ssl=required
ssl_cert = </certs/cert.crt
ssl_key = </certs/cert.key");

        File.WriteAllText(C.Paths.ConfigDataFor(MAIN), main.ToString());

    }
}