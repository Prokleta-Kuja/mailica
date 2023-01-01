# TODO

Add queues folder to keep mail if delivery fails, also add a job to process them later
Spam scoring - add it to SessionContext and build it incrementaly then check just before mailbox delivery
A recurring job to check if there are maildirs for deleted users and/or delete it when deleting users
A recurring job to delete logs
Master users cant login as themself
Test SMTP server gracefull shutdown. Should be console logged after docker stop
Reload certs https://doc.dovecot.org/admin_manual/doveadm_http_api/#doveadm-reload
Vite + react swc + bootstrap 5.3.0

# Important dovecot conf
- https://doc.dovecot.org/configuration_manual/lastlogin_plugin/

- https://doc.dovecot.org/settings/core/#core_setting-auth_master_user_separator
- https://doc.dovecot.org/settings/core/#core_setting-auth_username_chars
- https://doc.dovecot.org/settings/core/#core_setting-auth_username_format
- https://doc.dovecot.org/settings/core/#core_setting-auth_verbose
- https://doc.dovecot.org/settings/core/#core_setting-base_dir
- https://doc.dovecot.org/settings/core/#core_setting-imap_idle_notify_interval
- https://doc.dovecot.org/settings/core/#core_setting-listen
- https://doc.dovecot.org/settings/core/#core_setting-mail_location
- https://doc.dovecot.org/settings/core/#core_setting-mailbox_idle_check_interval
- https://doc.dovecot.org/settings/core/#core_setting-protocols
- https://doc.dovecot.org/settings/core/#core_setting-recipient_delimiter
- https://doc.dovecot.org/settings/core/#core_setting-ssl + odmah ispod za uri do certa

# Installation

Run mailica first to initialize dovecot configuration.
Create mailsolution folder, eg. /opt/appdata/mailica, with subfolders config & mail.

## mailica container

Bind crt and key files to /certs/cert.crt and /certs/cert.key
Bind mailsolution/config to /config
Bind mailsolution/mail to /mail

## dovecot container

Bind crt and key files to /certs/cert.crt and /certs/cert.key
Bind mailsolution/config to /etc/dovecot
Bind mailsolution/mail to /mail

# Development

On first run a data folder is going to be created and it going to fail because you need to put cert.crt & cert.key in data/certs folder.

# Other
After changing system user or password via env, dovcot configuration must be reloaded or dovecot restarted.