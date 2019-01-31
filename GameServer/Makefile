gameserver:game_server.cpp
	protoc --cpp_out=. msg.proto
	g++ -L/usr/lib64/mysql -lmysqlclient -lprotobuf -lpthread -o gameserver game_server.cpp msg.pb.cc
clean:
	rm gameserver msg.pb*
