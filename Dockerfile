FROM fsharp/fsharp:4.0.0.4
MAINTAINER pocketberserker

COPY ./bin/FSDN /app/FSDN

EXPOSE 8083

CMD ["mono", "/app/FSDN/FSDN.exe", "--home-directory", "/app/FSDN/"]
