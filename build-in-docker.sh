#!/bin/bash -eux

CONTAINER=fsdn-build
IMAGE=fsdn-build

BIN=./bin

if [ -e $BIN ]; then
  rm -rf $BIN
fi

mkdir -p $BIN/FSDN

docker build -t $CONTAINER -f Dockerfile-build .

docker rm $IMAGE || true

docker run -d --name=$IMAGE $CONTAINER

docker cp $IMAGE:/app/bin/FSDN.zip ./bin/

docker stop $IMAGE

docker rm $IMAGE || true
docker rmi $IMAGE

unzip -d ./bin/FSDN ./bin/FSDN.zip

docker build -t fsdn .

