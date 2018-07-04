@ECHO OFF
echo "Downloading Images for Cache"
docker pull nginx:alpine
REM redis:alpine mvertes/alpine-mongo kusmierz/beanstalkd bootlegger/transcode-server:latest bootlegger/server-app:latest
echo "Exporting Images"
docker save nginx:alpine -o images.tar
REM redis:alpine mvertes/alpine-mongo kusmierz/beanstalkd bootlegger/transcode-server:latest bootlegger/server-app:latest -o images.tar