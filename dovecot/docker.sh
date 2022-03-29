docker stop dovecot && docker rm dovecot
docker create \
  --name dovecot \
  --hostname dovecot \
  --net=gunda \
  -e TZ=Europe/Zagreb \
  -v ~/repos/mailica/dovecot/config:/etc/dovecot \
  -v ~/repos/mailica/dovecot/data:/srv/mail \
  -p 143:143 \
  -p 587:587 \
  -p 4190:4190 \
  dovecot/dovecot:2.3.18 
docker start dovecot && docker logs dovecot -f
