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

## Precondition

Please create relevant mysql database and tables for GameServer as below:

```
create database snowman;
```

```
CREATE TABLE `user`(
   `uid` int(11) NOT NULL AUTO_INCREMENT COMMENT '用户id',
   `username` varchar(255) NOT NULL COMMENT '用户名',
   `password` varchar(100) NOT NULL COMMENT '密码',
   `winnumbers` int(11) NOT NULL DEFAULT '0' COMMENT '胜利场次',
   `losenumbers` int(11) NOT NULL DEFAULT '0' COMMENT '失败场次',
   `winrate` int(11) NOT NULL DEFAULT '0' COMMENT '胜利概率',
   PRIMARY KEY ( `uid` ),
   UNIQUE KEY `username` (`username`),
   KEY `user_pwd_index` (`username`, `password`)
)ENGINE=InnoDB DEFAULT CHARSET=utf8;
```

## Run

```
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib
./gameserver
```

## TODO

* Network Reconnection
* Robustness
* Mysql Optimization

## Refs

* [C++使用protobuf(Linux下)](http://hahaya.info/use-protobuf-in-c-plus-plus)
* [protocolbuffers/protobuf](https://github.com/protocolbuffers/protobuf/blob/master/src/README.md)
