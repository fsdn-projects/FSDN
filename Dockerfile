FROM fsharp:4.1.0.1
MAINTAINER pocketberserker

COPY ./bin/FSDN /app/FSDN

EXPOSE 8083

CMD ["mono", "/app/FSDN/FSDN.exe", "--home-directory", "/app/FSDN/"]
