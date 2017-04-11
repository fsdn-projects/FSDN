FROM fsharp:4.1.0.1
MAINTAINER pocketberserker

RUN apt-get -y clean \
    && apt-get -y purge \
    && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/* \
    && curl -sL https://deb.nodesource.com/setup_7.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

COPY . /app
WORKDIR /app

RUN ./build.sh Pack

CMD ["mono", "/app/bin/FSDN/FSDN.exe", "--home-directory", "./bin/FSDN/"]
