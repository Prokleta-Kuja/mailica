docker stop dovecot && docker rm dovecot
docker create \
  --name dovecot \
  --hostname dovecot \
  --net=zzz \
  -e TZ=Europe/Zagreb \
  -v ~/repos/mailica/dovecot/config:/etc/dovecot \
  -v ~/repos/mailica/dovecot/data:/srv/mail \
  -v ~/repos/mailica/certs:/certs:ro \
  -v ~/repos/mailica/src/mailica/data:/data \
  -p 993:993 \
  -p 587:587 \
  dovecot/dovecot:2.3.20
docker start dovecot && docker logs dovecot -f
