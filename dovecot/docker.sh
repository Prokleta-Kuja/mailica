docker stop dovecot && docker rm dovecot
docker create \
  --name dovecot \
  --hostname dovecot \
  --net=zzz \
  -e TZ=Europe/Zagreb \
  -v ~/repos/mailica/src/mailica/data/config:/etc/dovecot \
  -v ~/repos/mailica/src/mailica/data/mail/:/mail \
  -v ~/repos/mailica/src/mailica/data/certs:/certs:ro \
  -p 993:993 \
  dovecot/dovecot:2.3.20
docker start dovecot && docker logs dovecot -f
