#include <unistd.h>
#include <fcntl.h>
#include <stdio.h>
#include <signal.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <errno.h>
#include <sys/shm.h>
#include <stdlib.h>
#include <sys/time.h>    
#include <sys/select.h>
#include <pthread.h>
#include<mysql/mysql.h>
#include "msg.pb.h"

#include <iostream>
using namespace std;

#define LISTENQ  1024  /* second argument to listen() */
#define LOGIC_FRAME 50 //logic frame on the side of server(ms)
#define FRAME_QUEUE_LEN 1000 //frame_queue len

#define Max(a,b) ((a>b)?(a):(b))
/* Simplifies calls to bind(), connect(), and accept() */
/* $begin sockaddrdef */
typedef struct sockaddr SA;
/* $end sockaddrdef */

//mysql connection
MYSQL *mysqlconn;
string user_table = "user";
string user_info_table = "user_info";

void mysql_with_error(MYSQL *con, string msg)
{
	fprintf(stderr, "%s: %s\n", msg.c_str(), mysql_error(con));
	mysql_close(con);
	exit(1);
}

void unix_error(char *msg) /* unix-style error */
{
	fprintf(stderr, "%s: %s\n", msg, strerror(errno));
	exit(-1);
}

/*
*   * open_listenfd - open and return a listening socket on port
*    *     Returns -1 and sets errno on Unix error.
*     */
/* $begin open_listenfd */
int open_listenfd(int port)
{
	int listenfd, optval = 1;
	struct sockaddr_in serveraddr;

	/* Create a socket descriptor */
	if ((listenfd = socket(AF_INET, SOCK_STREAM, 0)) < 0)
		return -1;

	/* Eliminates "Address already in use" error from bind. */
	if (setsockopt(listenfd, SOL_SOCKET, SO_REUSEADDR,
		(const void *)&optval, sizeof(int)) < 0)
		return -1;

	/* Listenfd will be an endpoint for all requests to port
	*        on any IP address for this host */
	bzero((char *)&serveraddr, sizeof(serveraddr));
	serveraddr.sin_family = AF_INET;
	serveraddr.sin_addr.s_addr = htonl(INADDR_ANY);
	serveraddr.sin_port = htons((unsigned short)port);
	if (bind(listenfd, (SA *)&serveraddr, sizeof(serveraddr)) < 0)
		return -1;

	/* Make it a listening socket ready to accept connection requests */
	if (listen(listenfd, LISTENQ) < 0)
		return -1;
	//zombies
	signal(SIGCHLD, SIG_IGN);
	return listenfd;
}
/* $end open_listenfd */

int Accept(int s, struct sockaddr *addr, socklen_t *addrlen)
{
	int rc;

	if ((rc = accept(s, addr, addrlen)) < 0)
	{
		unix_error("Accept error");
	}
	return rc;
}

int Open_listenfd(int port)
{
	int rc;

	if ((rc = open_listenfd(port)) < 0)
	{
		unix_error("Open_listenfd error");
	}
	return rc;
}


float timedifference_msec(struct timeval t0, struct timeval t1)
{
	return (t1.tv_sec - t0.tv_sec) * 1000.0f + (t1.tv_usec - t0.tv_usec) / 1000.0f;
}


CodeBattle::Client_Frame frame_queue[FRAME_QUEUE_LEN];
int frame_queue_index = 0;
int frame_seq = 0;

typedef unsigned char BYTE;

CodeBattle::Client_Frame BytesToClient_Frame(BYTE *bytes, int size) {
	CodeBattle::Client_Frame clientframe;
	clientframe.ParseFromArray(bytes, size);
	return clientframe;
}
CodeBattle::Server_Frame BytesToServer_Frame(BYTE *bytes,int size) {
	CodeBattle::Server_Frame serverframe;
	serverframe.ParseFromArray(bytes, size);
	return serverframe;
}
CodeBattle::Login_Frame BytesToLogin_Frame(BYTE *bytes, int size) {
	CodeBattle::Login_Frame loginframe;
	loginframe.ParseFromArray(bytes, size);
	return loginframe;
}

//host sequence to net sequence
void uint_to_bytes(unsigned int uint, BYTE* bytes) {
	//BYTE* bytes = new BYTE[4];
	bytes[3] = uint & 0x000000ff;
	bytes[2] = (uint >> 8) & 0x000000ff;
	bytes[1] = (uint >> 16) & 0x000000ff;
	bytes[0] = (uint >> 24) & 0x000000ff;
	//return bytes;
}
//net sequence to host sequence
unsigned int bytes_to_uint(BYTE* bytes) {
	unsigned int uint = bytes[3];
	uint |= (bytes[2] << 8);
	uint |= (bytes[1] << 16);
	uint |= (bytes[0] << 24);
	return uint;
}

void Send(int pre_fd, int later_fd, CodeBattle::Server_Frame serverframe) {
	//Serialize Server_Frame
	unsigned int size = serverframe.ByteSize();
	BYTE* bytes = new BYTE[size+4];
	//Serialize len
	uint_to_bytes(size, bytes);
	//Serialize To Array
	serverframe.SerializeToArray(bytes+4, size);
	//Send msg
	int n = send(pre_fd, bytes, size + 4, 0);
	if (n != (size + 4)) {
		fprintf(stderr, "invalid send len:%d, and should be:%d\n", n, size + 4);
	}
	n = send(later_fd, bytes, size + 4, 0);
	if (n != (size + 4)) {
		fprintf(stderr, "invalid send len:%d, and should be:%d\n", n, size + 4);
	}
}

void ReceiveMessage(int fd) {
	//...
	BYTE* lenBytes = new BYTE[4];
	int rec = recv(fd, lenBytes, 4, 0);
	if (rec != 4)
	{
		//throw new Exception("Remote Closed the connection");
		fprintf(stderr, "invalid recv len:%d, and should be:%d\n", rec, 4);
	}
	//msg len get
	unsigned int len = bytes_to_uint(lenBytes);
	BYTE* data = new BYTE[len];
	rec = recv(fd, data, len, 0);
	if (rec != len)
	{
		//throw new Exception("Remote Closed the connection");
		fprintf(stderr, "invalid recv msg len:%d, and should be:%d\n", rec, len);
	}
	//Bytes To Client_Frame
	CodeBattle::Client_Frame clientframe = BytesToClient_Frame(data,len);
	frame_queue[frame_queue_index] = clientframe;
	frame_queue_index++;
}

fd_set rset;
//10 ms(relevant to client)
struct timeval recv_time = { 0, 10000 };

//recv the frame
void Recv_Frame(int pre_fd, int later_fd) {
	//select
	FD_SET(pre_fd, &rset);
	FD_SET(later_fd, &rset);
	int maxfdp1 = Max(pre_fd, later_fd) + 1;
	select(maxfdp1, &rset, NULL, NULL, &recv_time);
	//read
	if (FD_ISSET(pre_fd, &rset)) {
		ReceiveMessage(pre_fd);
		//n = recv(fd, buf + pivot, size - pivot, 0);

	}
	if (FD_ISSET(later_fd, &rset)) {
		ReceiveMessage(later_fd);
	}
}

//send the frame
void Send_Frame(int pre_fd, int later_fd) {
	int i, j;
	CodeBattle::Server_Frame serverframe;
	//construct the server_frame
	if (frame_queue_index)
	{
		serverframe.set_empty(false);

		CodeBattle::Client_Frame* preframe = serverframe.mutable_preframe();
		CodeBattle::Client_Frame* laterframe = serverframe.mutable_laterframe();
		//init the laterframe direction
		preframe->set_direction(-1);
		laterframe->set_direction(-1);
		//init the ip
		preframe->set_ip("");
		laterframe->set_ip("");
		for (j = 0; j < frame_queue_index; j++) {
			if (preframe->ip() == "") {
				//init
				preframe->set_ip(frame_queue[j].ip());
				preframe->set_direction(frame_queue[j].direction());
			}
			else if (preframe->ip() == frame_queue[j].ip()) {
				//parse
				preframe->set_direction(frame_queue[j].direction());
			}
			else if (laterframe->ip() == "") {
				//init
				laterframe->set_ip(frame_queue[j].ip());
				laterframe->set_direction(frame_queue[j].direction());
			}
			else if (laterframe->ip() == frame_queue[j].ip()) {
				//parse
				laterframe->set_direction(frame_queue[j].direction());
			}
			else {
				fprintf(stderr, "invalid frame\n");
			}
		}
	}
	else {
		serverframe.set_empty(true);
		CodeBattle::Client_Frame* preframe = serverframe.mutable_preframe();
		CodeBattle::Client_Frame* laterframe = serverframe.mutable_laterframe();
		preframe->set_ip("127.0.0.1");
		preframe->set_direction(-1);
		laterframe->set_ip("127.0.0.1");
		laterframe->set_direction(-1);
	}
	//reset receive queue index
	frame_queue_index = 0;
	serverframe.set_frameseq(frame_seq);
	frame_seq++;
	//send the server_frame
	Send(pre_fd, later_fd, serverframe);
}

//process the room pvp
void process_room(int pre_fd, int later_fd) {
	/*
	if (fcntl(pre_fd, F_SETFL, fcntl(pre_fd, F_GETFL) | O_NONBLOCK) < 0) {
	fprintf(stderr, "fcntl failure\n");
	}
	if (fcntl(later_fd, F_SETFL, fcntl(later_fd, F_GETFL) | O_NONBLOCK) < 0) {
	fprintf(stderr, "fcntl failure\n");
	}
	*/
	//reset rset
	FD_ZERO(&rset);
	struct timeval update_time, current_time;
	gettimeofday(&update_time, 0);
	while (1) {
		//receive queue
		Recv_Frame(pre_fd, later_fd);
		//
		gettimeofday(&current_time, 0);
		if (timedifference_msec(update_time, current_time) >= LOGIC_FRAME) {
			//send queue
			Send_Frame(pre_fd, later_fd);
			gettimeofday(&update_time, 0);
		}
	}
}

bool is_mysql_exist(string sql) {
	// mysql_query()ִ�гɹ�����0��ʧ�ܷ��ط�0ֵ����PHP�в�һ��  
	if (mysql_query(mysqlconn, sql.c_str()))
	{
		mysql_with_error(mysqlconn, "mysql_query failure");	
	}
	MYSQL_RES *result = mysql_store_result(mysqlconn);
	if (result == NULL)
	{
		mysql_with_error(mysqlconn, "mysql_store_result failure");
	}
	MYSQL_ROW row = mysql_fetch_row(result);
	if (row) {
		mysql_free_result(result);
		return true;
	}
	else {
		mysql_free_result(result);
		return false;
	}
}

//get user info data
CodeBattle::Userinfo_Frame get_mysql_data(string sql) {
	// mysql_query()ִ�гɹ�����0��ʧ�ܷ��ط�0ֵ����PHP�в�һ��  
	if (mysql_query(mysqlconn, sql.c_str()))
	{
		mysql_with_error(mysqlconn, "mysql_query failure");
	}
	MYSQL_RES *result = mysql_store_result(mysqlconn);
	if (result == NULL)
	{
		mysql_with_error(mysqlconn, "mysql_store_result failure");
	}
	MYSQL_ROW row = mysql_fetch_row(result);
	if (!row) {
		fprintf(stderr, "%s query result empty\n", sql.c_str());
		mysql_close(mysqlconn);
		exit(1);
	}
	unsigned int num_fields = mysql_num_fields(result);
	MYSQL_FIELD *fields = mysql_fetch_fields(result);
	CodeBattle::Userinfo_Frame userinfoframe;
	for (int i = 0; i < num_fields; i++)
	{
		string field_name = fields[i].name;
		if (field_name == "uid") {
			userinfoframe.set_id(atoi(row[i]));
		}
		else if (field_name == "username") {
			userinfoframe.set_username(row[i]);
		}
		else if (field_name == "winnumbers") {
			userinfoframe.set_winnumbers(atoi(row[i]));
		}
		else if (field_name == "losenumbers") {
			userinfoframe.set_losenumbers(atoi(row[i]));
		}
		else if (field_name == "winrate") {
			userinfoframe.set_winrate(atoi(row[i]));
		}
	}
	mysql_free_result(result);
	return userinfoframe;
}

//update sql statement
void updateSQL(string sql) {
	// mysql_query()ִ�гɹ�����0��ʧ�ܷ��ط�0ֵ����PHP�в�һ��  
	if (mysql_query(mysqlconn, sql.c_str()))
	{
		mysql_with_error(mysqlconn, "update mysql_query failure");
	}
}

//process register
bool process_register(int fd, CodeBattle::Login_Frame loginframe) {
	//check validation client-side
	char query_sql[100];
	CodeBattle::Login_Response loginresponse;
	//check whether username has existed already
	sprintf(query_sql, "select * from %s where username = '%s'", user_table.c_str(), (loginframe.username()).c_str());
	bool flag = true;
	if (is_mysql_exist(query_sql)) {
		loginresponse.set_succeed(false);
		loginresponse.set_errcode(1);
		printf("username: %s has existed already!\n", (loginframe.username()).c_str());
		flag = false;
	}
	else {
		//insert into user table
		sprintf(query_sql, "insert into %s (username,password) values ('%s','%s')", user_table.c_str(), (loginframe.username()).c_str(), (loginframe.password()).c_str());
		updateSQL(query_sql);
		//insert into user_info table
		sprintf(query_sql, "insert into %s (username,winnumbers,losenumbers,winrate) values ('%s', 0, 0, 0)", user_info_table.c_str(), (loginframe.username()).c_str());
		updateSQL(query_sql);
		//set response
		loginresponse.set_succeed(true);
		loginresponse.set_errcode(0);
		printf("username: %s register successfully!\n", (loginframe.username()).c_str());
	}
	//send response
	//Serialize Login_Frame
	unsigned int size = loginresponse.ByteSize();
	BYTE* bytes = new BYTE[size + 4];
	//Serialize len
	uint_to_bytes(size, bytes);
	//Serialize To Array
	loginresponse.SerializeToArray(bytes + 4, size);
	//Send msg
	int n = send(fd, bytes, size + 4, 0);
	if (n != (size + 4)) {
		fprintf(stderr, "invalid send len:%d, and should be:%d\n", n, size + 4);
	}
	delete[]bytes;
	if (!flag)
		return false;
	return true;
}

//process login(logical function)
void process_login(int fd, CodeBattle::Login_Frame loginframe) {
	//receive the login information
	if (!loginframe.login()) {
		BYTE* lenBytes = new BYTE[4];
		int rec = recv(fd, lenBytes, 4, 0);
		if (rec != 4)
		{
			//throw new Exception("Remote Closed the connection");
			fprintf(stderr, "invalid recv len:%d, and should be:%d\n", rec, 4);
		}
		//msg len get
		unsigned int len = bytes_to_uint(lenBytes);
		BYTE* data = new BYTE[len];
		rec = recv(fd, data, len, 0);
		if (rec != len)
		{
			//throw new Exception("Remote Closed the connection");
			fprintf(stderr, "invalid recv msg len:%d, and should be:%d\n", rec, len);
		}
		//Bytes To Client_Frame
		loginframe = BytesToLogin_Frame(data, len);
		delete[]lenBytes;
		delete[]data;
	}

	//common process
	int errcode = 0;
	//check user exist
	char query_sql[100];
	sprintf(query_sql, "select * from %s where username = '%s'", user_table.c_str(), (loginframe.username()).c_str());
	if (!is_mysql_exist(query_sql)) {
		errcode = 1;
	}
	else {
		sprintf(query_sql, "select * from %s where username = '%s' and password = '%s'", user_table.c_str(), (loginframe.username()).c_str(), (loginframe.password()).c_str());
		if (!is_mysql_exist(query_sql)) {
			errcode = 2;
		}
	}
	//send the login response
	//construct the login response
	CodeBattle::Login_Response loginresponse;
	if (!errcode) {
		loginresponse.set_succeed(true);
	}
	else {
		loginresponse.set_succeed(false);
	}
	loginresponse.set_errcode(errcode);
	//send login response
	//Serialize Login_Frame
	unsigned int size = loginresponse.ByteSize();
	BYTE* bytes = new BYTE[size + 4];
	//Serialize len
	uint_to_bytes(size, bytes);
	//Serialize To Array
	loginresponse.SerializeToArray(bytes + 4, size);
	//Send msg
	int n = send(fd, bytes, size + 4, 0);
	if (n != (size + 4)) {
		fprintf(stderr, "invalid send len:%d, and should be:%d\n", n, size + 4);
	}
	if (errcode) {
		//login failure
		return;
	}
	//login succeed
	sprintf(query_sql, "select * from %s where username = '%s'", user_info_table.c_str(), (loginframe.username()).c_str());
	CodeBattle::Userinfo_Frame userinfoframe = get_mysql_data(query_sql);
	//send the user_info
	size = userinfoframe.ByteSize();
	BYTE* info_bytes = new BYTE[size + 4];
	//Serialize len
	uint_to_bytes(size, info_bytes);
	//Serialize To Array
	userinfoframe.SerializeToArray(info_bytes + 4, size);
	//Send msg
	n = send(fd, info_bytes, size + 4, 0);
	if (n != (size + 4)) {
		fprintf(stderr, "invalid send len:%d, and should be:%d\n", n, size + 4);
	}

	//delete
	delete[]bytes;
	delete[]info_bytes;
}

//main entry of register and login
void pre_process_login(int fd) {
	//receive the login information
	BYTE* lenBytes = new BYTE[4];
	int rec = recv(fd, lenBytes, 4, 0);
	if (rec != 4)
	{
		//throw new Exception("Remote Closed the connection");
		fprintf(stderr, "invalid recv len:%d, and should be:%d\n", rec, 4);
	}
	//msg len get
	unsigned int len = bytes_to_uint(lenBytes);
	BYTE* data = new BYTE[len];
	rec = recv(fd, data, len, 0);
	if (rec != len)
	{
		//throw new Exception("Remote Closed the connection");
		fprintf(stderr, "invalid recv msg len:%d, and should be:%d\n", rec, len);
	}
	//Bytes To Client_Frame
	CodeBattle::Login_Frame loginframe = BytesToLogin_Frame(data, len);
	if (!loginframe.login()) {
		//register
		if (!process_register(fd, loginframe)) {
			//delete
			delete[]lenBytes;
			delete[]data;
			return;
		}
	}
	//login
	process_login(fd, loginframe);
	//delete
	delete[]lenBytes;
	delete[]data;
}

void *login_thread(void *fd_point)
{
	int *free_fd = (int *)fd_point;
	int fd = *free_fd;
	//free connfd
	free(free_fd);
	//process the login
	pre_process_login(fd);
	//close fd
	close(fd);
	return NULL;
}

//process login(mutil threading)
void login_process() {
	int listen_port = 8080, listenfd;
	listenfd = Open_listenfd(listen_port);
	while (1) {
		struct sockaddr_in clientaddr;
		socklen_t clientlen;

		//protect the connfd variable
		int *connfd = (int *)malloc(sizeof(int));

		*connfd = Accept(listenfd, (SA *)&clientaddr, &clientlen);
		printf("Login Server:Received from %s:%d\n", inet_ntoa(clientaddr.sin_addr), ntohs(clientaddr.sin_port));
		pthread_t tid;
		int ret = pthread_create(&tid, NULL, login_thread, (void *)(connfd));
		if (ret != 0) {
			fprintf(stderr, "Create pthread error!\n");
			exit(1);
		}
		//detach thread
		ret = pthread_detach(tid);
		if (ret != 0) {
			fprintf(stderr, "pthread_detach failure!\n");
			exit(1);
		}
	}
}

void Connect_DB(string host, string user, string pwd, string db_name)
{
	// ����mysql_real_connect����һ�����ݿ�����  
	// �ɹ�����MYSQL*���Ӿ����ʧ�ܷ���NULL  
	mysqlconn = mysql_real_connect(mysqlconn, host.c_str(),
		user.c_str(), pwd.c_str(), db_name.c_str(), 0, NULL, 0);
	if (mysqlconn == NULL)
	{
		mysql_with_error(mysqlconn, "mysql_real_connect failure");
	}
	printf("Connect Mysql successfully...\n");
}

void Init_mysql_connection() {
	mysqlconn = mysql_init(NULL); // ��ʼ�����ݿ����ӱ���  
	if (mysqlconn == NULL)
	{
		fprintf(stderr, "mysql_init failure: %s\n", mysql_error(mysqlconn));
		exit(1);
	}
	//connect to db
	Connect_DB("localhost", "root", "myroot", "snowman");
}

int main() {
	//init mysql connection
	Init_mysql_connection();
	//init
	int pro_index = -1;
	//listen socket
	pid_t pid;
	int listen_port = 80, listenfd, connfd, tmpfd;
	pid = fork();
	if (pid<0) {
		unix_error("fork failure");
	}
	else if (!pid) {
		//process login(single process)
		printf("Process:%d Accepting Login connections ... \n", getpid());
		login_process();
		exit(0);
	}
	listenfd = Open_listenfd(listen_port);
	printf("Process:%d Accepting Session connections ... \n", getpid());
	while (1) {
		struct sockaddr_in clientaddr;
		socklen_t clientlen;
		connfd = Accept(listenfd, (SA *)&clientaddr, &clientlen);
		printf("Session Server:Received from %s:%d\n", inet_ntoa(clientaddr.sin_addr), ntohs(clientaddr.sin_port));
		pro_index = (pro_index + 1) % 1000;
		if (pro_index % 2 == 0) {
			tmpfd = connfd;
		}
		else {
			pid = fork();
			if (pid < 0) {
				unix_error("fork failure");
			}
			else if (pid) {
				close(tmpfd);
				close(connfd);
			}
			else {
				close(listenfd);
				process_room(tmpfd, connfd);
				exit(0);
			}
		}
	}
	fprintf(stderr, "Error: Never come here\n");
	return -1;
}