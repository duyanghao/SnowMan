# GameServer
SnowMan GameServer...

## Architecture Diagram

![](https://raw.githubusercontent.com/duyanghao/SnowMan/master/images/gameserver_arch.png)

## Language

```
C/C++
```

## Build

```
yum install -y autoconf automake libtool unzip
unzip protobuf-3.1.0.zip
cd protobuf-3.1.0
./autogen.sh
./configure
make
make check
sudo make install

yum install -y mysql mysql-server mysql-devel
cd ../ && make
```

## TODO

* Network Reconnection
* Robustness
