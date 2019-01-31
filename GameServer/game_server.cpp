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
#include <mysql/mysql.h>
#include "msg.pb.h"

#include <iostream>
#include <map>
using namespace std;

#define LISTENQ  1024  /* second argument to listen() */
#define LOGIC_FRAME 70 //logic frame on the side of server(ms)
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

//map for finish room
map <string, string> ip_username;
string room_ip_store[2];

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
int frame_seq = 1;

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

//mysql area
bool is_mysql_exist(string sql) {
	// mysql_query()执行成功返回0，失败返回非0值。与PHP中不一样  
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
	// mysql_query()执行成功返回0，失败返回非0值。与PHP中不一样  
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
	userinfoframe.set_ip("");
	mysql_free_result(result);
	return userinfoframe;
}

//update sql statement
void updateSQL(string sql) {
	// mysql_query()执行成功返回0，失败返回非0值。与PHP中不一样  
	if (mysql_query(mysqlconn, sql.c_str()))
	{
		mysql_with_error(mysqlconn, "update mysql_query failure");
	}
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

string envir_choose_ip;
//flag for finish the room
bool is_died = false;
string died_ip = "";

//init serverframe
CodeBattle::Server_Frame init_serverframe() {
	CodeBattle::Server_Frame serverframe;
	//empty
	serverframe.set_empty(true);
	//seq
	serverframe.set_frameseq(-1);

	//preframe and laterframe
	CodeBattle::Single_Frame* preframe = serverframe.mutable_preframe();
	CodeBattle::Single_Frame* laterframe = serverframe.mutable_laterframe();
	//ip
	preframe->set_ip("");
	laterframe->set_ip("");
	//died
	preframe->set_died(false);
	laterframe->set_died(false);
	//move
	preframe->set_moved(false);
	laterframe->set_moved(false);
	//direction
	CodeBattle::Move_Direction* predir = preframe->mutable_direction();
	CodeBattle::Move_Direction* laterdir = laterframe->mutable_direction();
	predir->set_left(false);
	predir->set_right(false);
	predir->set_up(false);
	laterdir->set_left(false);
	laterdir->set_right(false);
	laterdir->set_up(false);
	//hpchanged
	preframe->set_hpchanged(false);
	laterframe->set_hpchanged(false);
	//playerhp and enemyhp
	//preframe playerhp and enemyhp
	CodeBattle::Hp_Object* preplayerhp = preframe->mutable_playerhp();
	CodeBattle::Hp_Object* preenemyhp = preframe->mutable_enemyhp();
	preplayerhp->set_ischanged(false);
	preplayerhp->set_changevalue(0);
	preenemyhp->set_ischanged(false);
	preenemyhp->set_changevalue(0);
	//laterframe playerhp and enemyhp
	CodeBattle::Hp_Object* laterplayerhp = laterframe->mutable_playerhp();
	CodeBattle::Hp_Object* laterenemyhp = laterframe->mutable_enemyhp();
	laterplayerhp->set_ischanged(false);
	laterplayerhp->set_changevalue(0);
	laterenemyhp->set_ischanged(false);
	laterenemyhp->set_changevalue(0);
	//snow
	CodeBattle::Generated_Object* presnow = preframe->mutable_snow();
	CodeBattle::Generated_Object* latersnow = laterframe->mutable_snow();
	presnow->set_isgenerated(false);
	latersnow->set_isgenerated(false);
	CodeBattle::Generated_Position* prepos = presnow->mutable_pos();
	CodeBattle::Generated_Position* laterpos = latersnow->mutable_pos();
	prepos->set_x(0);
	prepos->set_y(0);
	prepos->set_z(0);
	laterpos->set_x(0);
	laterpos->set_y(0);
	laterpos->set_z(0);
	
	//common frame
	CodeBattle::Common_Frame* comframe = serverframe.mutable_comframe();
	//generated
	comframe->set_generated(false);
	//choose ip
	comframe->set_chooseip(envir_choose_ip);
	//animal
	CodeBattle::Generated_Object* comframeanimal = comframe->mutable_animal();
	comframeanimal->set_isgenerated(false);
	CodeBattle::Generated_Position* animalpos = comframeanimal->mutable_pos();
	animalpos->set_x(0);
	animalpos->set_y(0);
	animalpos->set_z(0);
	//bird
	CodeBattle::Generated_Object* comframebird = comframe->mutable_bird();
	comframebird->set_isgenerated(false);
	CodeBattle::Generated_Position* birdpos = comframebird->mutable_pos();
	birdpos->set_x(0);
	birdpos->set_y(0);
	birdpos->set_z(0);
	//food
	CodeBattle::Generated_Object* comframefood = comframe->mutable_food();
	comframefood->set_isgenerated(false);
	CodeBattle::Generated_Position* foodpos = comframefood->mutable_pos();
	foodpos->set_x(0);
	foodpos->set_y(0);
	foodpos->set_z(0);

	return serverframe;
}


//bool is_died=false;

//send the frame
void Send_Frame(int pre_fd, int later_fd) {
	CodeBattle::Server_Frame serverframe = init_serverframe();
	//construct the server_frame
	if (frame_queue_index)
	{
		//not empty
		serverframe.set_empty(false);
		//produce preframe, laterframe and commonframe
		CodeBattle::Single_Frame* preframe = serverframe.mutable_preframe();
		CodeBattle::Single_Frame* laterframe = serverframe.mutable_laterframe();
		CodeBattle::Common_Frame* comframe = serverframe.mutable_comframe();
		//loop queue
		for (int i = 0; i < frame_queue_index; i++) {
			if (preframe->ip() == "") {
				//init
				preframe->set_ip(frame_queue[i].ip());
				preframe->set_died(frame_queue[i].died());
				//died
				if (preframe->died()) {
					is_died = true;
					died_ip = frame_queue[i].ip();
					break;
				}
				preframe->set_moved(frame_queue[i].moved());
				//moved
				if (preframe->moved()) {
					CodeBattle::Move_Direction* tmpdir1 = preframe->mutable_direction();
					CodeBattle::Move_Direction* tmpdir2 = frame_queue[i].mutable_direction();
					tmpdir1->set_left(tmpdir2->left());
					tmpdir1->set_right(tmpdir2->right());
					tmpdir1->set_up(tmpdir2->up());
				}
				preframe->set_hpchanged(frame_queue[i].hpchanged());
				//hp changed
				if (preframe->hpchanged()) {
					//player
					if (frame_queue[i].playertype()) {
						CodeBattle::Hp_Object* tmphp = preframe->mutable_playerhp();
						tmphp->set_ischanged(true);
						tmphp->set_changevalue(frame_queue[i].changevalue());
					}
					else { //enemy
						CodeBattle::Hp_Object* tmphp = preframe->mutable_enemyhp();
						tmphp->set_ischanged(true);
						tmphp->set_changevalue(frame_queue[i].changevalue());
					}
				}
				//generated
				if (frame_queue[i].generated()) {
					switch (frame_queue[i].objecttype()) {
						case 1: {
							//1=snow (Overwrite strategy)
							CodeBattle::Generated_Object* tmpsnow = preframe->mutable_snow();
							tmpsnow->set_isgenerated(true);
							CodeBattle::Generated_Position* tmppos1 = tmpsnow->mutable_pos();
							CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
							tmppos1->set_x(tmppos2->x());
							tmppos1->set_y(tmppos2->y());
							tmppos1->set_z(tmppos2->z());
							break;
						}
						case 2: {
							//one side(client) (Overwrite strategy)
							//animal
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframeanimal = comframe->mutable_animal();
								comframeanimal->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframeanimal->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 3: {
							//bird
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframebird = comframe->mutable_bird();
								comframebird->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframebird->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 4: {
							//food
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframefood = comframe->mutable_food();
								comframefood->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframefood->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						default: {
							fprintf(stderr, "invalid generated type:%d\n", frame_queue[i].objecttype());
							break;
						}
					}
				}
				
			}
			else if (preframe->ip() == frame_queue[i].ip()) {
				//parse
				//init
				preframe->set_died(frame_queue[i].died());
				//died
				if (preframe->died()) {
					is_died = true;
					died_ip = frame_queue[i].ip();
					break;
				}
				if (frame_queue[i].moved()) {
					preframe->set_moved(frame_queue[i].moved());
					//moved(Overwrite strategy)
					if (preframe->moved()) {
						CodeBattle::Move_Direction* tmpdir1 = preframe->mutable_direction();
						CodeBattle::Move_Direction* tmpdir2 = frame_queue[i].mutable_direction();
						tmpdir1->set_left(tmpdir2->left());
						tmpdir1->set_right(tmpdir2->right());
						tmpdir1->set_up(tmpdir2->up());
					}

				}
				if (frame_queue[i].hpchanged()) {
					preframe->set_hpchanged(frame_queue[i].hpchanged());
					//hp changed(Accumulative strategy)
					if (preframe->hpchanged()) {
						//player
						if (frame_queue[i].playertype()) {
							CodeBattle::Hp_Object* tmphp = preframe->mutable_playerhp();
							tmphp->set_ischanged(true);
							tmphp->set_changevalue(frame_queue[i].changevalue() + tmphp->changevalue());
						}
						else { //enemy
							CodeBattle::Hp_Object* tmphp = preframe->mutable_enemyhp();
							tmphp->set_ischanged(true);
							tmphp->set_changevalue(frame_queue[i].changevalue() + tmphp->changevalue());
						}
					}
				}
				//generated
				if (frame_queue[i].generated()) {
					switch (frame_queue[i].objecttype()) {
						case 1: {
							//1=snow(Overwrite strategy)
							CodeBattle::Generated_Object* tmpsnow = preframe->mutable_snow();
							tmpsnow->set_isgenerated(true);
							CodeBattle::Generated_Position* tmppos1 = tmpsnow->mutable_pos();
							CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
							tmppos1->set_x(tmppos2->x());
							tmppos1->set_y(tmppos2->y());
							tmppos1->set_z(tmppos2->z());
							break;
						}
						case 2: {
							//one side(client) (Overwrite strategy)
							//animal
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframeanimal = comframe->mutable_animal();
								comframeanimal->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframeanimal->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 3: {
							//bird
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframebird = comframe->mutable_bird();
								comframebird->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframebird->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 4: {
							//food
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframefood = comframe->mutable_food();
								comframefood->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframefood->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						default: {
							fprintf(stderr, "invalid generated type:%d\n", frame_queue[i].objecttype());
							break;
						}
					}
				}
			}
			else if (laterframe->ip() == "") {
				//init
				laterframe->set_ip(frame_queue[i].ip());
				laterframe->set_died(frame_queue[i].died());
				//died
				if (laterframe->died()) {
					is_died = true;
					died_ip = frame_queue[i].ip();
					break;
				}
				laterframe->set_moved(frame_queue[i].moved());
				//moved
				if (laterframe->moved()) {
					CodeBattle::Move_Direction* tmpdir1 = laterframe->mutable_direction();
					CodeBattle::Move_Direction* tmpdir2 = frame_queue[i].mutable_direction();
					tmpdir1->set_left(tmpdir2->left());
					tmpdir1->set_right(tmpdir2->right());
					tmpdir1->set_up(tmpdir2->up());
				}
				laterframe->set_hpchanged(frame_queue[i].hpchanged());
				//hp changed
				if (laterframe->hpchanged()) {
					//player
					if (frame_queue[i].playertype()) {
						CodeBattle::Hp_Object* tmphp = laterframe->mutable_playerhp();
						tmphp->set_ischanged(true);
						tmphp->set_changevalue(frame_queue[i].changevalue());
					}
					else { //enemy
						CodeBattle::Hp_Object* tmphp = laterframe->mutable_enemyhp();
						tmphp->set_ischanged(true);
						tmphp->set_changevalue(frame_queue[i].changevalue());
					}
				}
				//generated
				if (frame_queue[i].generated()) {
					switch (frame_queue[i].objecttype()) {
						case 1: {
							//1=snow (Overwrite strategy)
							CodeBattle::Generated_Object* tmpsnow = laterframe->mutable_snow();
							tmpsnow->set_isgenerated(true);
							CodeBattle::Generated_Position* tmppos1 = tmpsnow->mutable_pos();
							CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
							tmppos1->set_x(tmppos2->x());
							tmppos1->set_y(tmppos2->y());
							tmppos1->set_z(tmppos2->z());
							break;
						}
						case 2: {
							//one side(client) (Overwrite strategy)
							//animal
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframeanimal = comframe->mutable_animal();
								comframeanimal->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframeanimal->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 3: {
							//bird
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframebird = comframe->mutable_bird();
								comframebird->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframebird->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 4: {
							//food
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframefood = comframe->mutable_food();
								comframefood->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframefood->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						default: {
							fprintf(stderr, "invalid generated type:%d\n", frame_queue[i].objecttype());
							break;
						}
					}
				}
				
			}
			else if (laterframe->ip() == frame_queue[i].ip()) {
				//parse
				//init
				laterframe->set_died(frame_queue[i].died());
				//died
				if (laterframe->died()) {
					is_died = true;
					died_ip = frame_queue[i].ip();
					break;
				}
				if (frame_queue[i].moved()) {
					laterframe->set_moved(frame_queue[i].moved());
					//moved(Overwrite strategy)
					if (laterframe->moved()) {
						CodeBattle::Move_Direction* tmpdir1 = laterframe->mutable_direction();
						CodeBattle::Move_Direction* tmpdir2 = frame_queue[i].mutable_direction();
						tmpdir1->set_left(tmpdir2->left());
						tmpdir1->set_right(tmpdir2->right());
						tmpdir1->set_up(tmpdir2->up());
					}

				}
				if (frame_queue[i].hpchanged()) {
					laterframe->set_hpchanged(frame_queue[i].hpchanged());
					//hp changed(Accumulative strategy)
					if (laterframe->hpchanged()) {
						//player
						if (frame_queue[i].playertype()) {
							CodeBattle::Hp_Object* tmphp = laterframe->mutable_playerhp();
							tmphp->set_ischanged(true);
							tmphp->set_changevalue(frame_queue[i].changevalue() + tmphp->changevalue());
						}
						else { //enemy
							CodeBattle::Hp_Object* tmphp = laterframe->mutable_enemyhp();
							tmphp->set_ischanged(true);
							tmphp->set_changevalue(frame_queue[i].changevalue() + tmphp->changevalue());
						}
					}
				}
				//generated
				if (frame_queue[i].generated()) {
					switch (frame_queue[i].objecttype()) {
						case 1: {
							//1=snow(Overwrite strategy)
							CodeBattle::Generated_Object* tmpsnow = laterframe->mutable_snow();
							tmpsnow->set_isgenerated(true);
							CodeBattle::Generated_Position* tmppos1 = tmpsnow->mutable_pos();
							CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
							tmppos1->set_x(tmppos2->x());
							tmppos1->set_y(tmppos2->y());
							tmppos1->set_z(tmppos2->z());
							break;
							}
						case 2: {
							//one side(client) (Overwrite strategy)
							//animal
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframeanimal = comframe->mutable_animal();
								comframeanimal->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframeanimal->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 3: {
							//bird
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframebird = comframe->mutable_bird();
								comframebird->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframebird->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						case 4: {
							//food
							if (frame_queue[i].ip() == envir_choose_ip) {
								comframe->set_generated(true);
								CodeBattle::Generated_Object* comframefood = comframe->mutable_food();
								comframefood->set_isgenerated(true);
								CodeBattle::Generated_Position* tmppos1 = comframefood->mutable_pos();
								CodeBattle::Generated_Position* tmppos2 = frame_queue[i].mutable_pos();
								tmppos1->set_x(tmppos2->x());
								tmppos1->set_y(tmppos2->y());
								tmppos1->set_z(tmppos2->z());
							}
							break;
						}
						default: {
							fprintf(stderr, "invalid generated type:%d\n", frame_queue[i].objecttype());
							break;
						}
					}
				}
			}
			else {
				fprintf(stderr, "invalid ip frame\n");
			}
		}
		//produce hp(relevant)
		/*//Larger strategy
		if (preframe->hpchanged() && laterframe->hpchanged()) {
			CodeBattle::Hp_Object* tmphp1 = preframe->mutable_playerhp();
			CodeBattle::Hp_Object* tmphp2 = laterframe->mutable_enemyhp();
			if (tmphp1->ischanged() && tmphp2->ischanged()) {
				//get the bigger hp change
				if ((tmphp2->changevalue()) > (tmphp1->changevalue())) {
					tmphp1->set_changevalue(tmphp2->changevalue());
				}
			}
			if (!tmphp1->ischanged() && tmphp2->ischanged()) {
				tmphp1->set_ischanged(true);
				tmphp1->set_changevalue(tmphp2->changevalue());
			}
			tmphp1 = preframe->mutable_enemyhp();
			tmphp2 = laterframe->mutable_playerhp();
			if (tmphp1->ischanged() && tmphp2->ischanged()) {
				//get the bigger hp change
				if ((tmphp2->changevalue()) > (tmphp1->changevalue())) {
					tmphp1->set_changevalue(tmphp2->changevalue());
				}
			}
			if (!tmphp1->ischanged() && tmphp2->ischanged()) {
				tmphp1->set_ischanged(true);
				tmphp1->set_changevalue(tmphp2->changevalue());
			}
			laterframe->set_hpchanged(false);
		}*/
		//one-side strategy
		if (preframe->ip() != envir_choose_ip) {
			preframe->set_hpchanged(false);
		}
		if(laterframe->ip() != envir_choose_ip){
			laterframe->set_hpchanged(false);
		}
	}
	else {
		serverframe.set_empty(true);
	}
	//reset receive queue index
	frame_queue_index = 0;
	//set frame seq
	serverframe.set_frameseq(frame_seq);
	frame_seq++;
	//send the server_frame
	Send(pre_fd, later_fd, serverframe);
}

CodeBattle::Login_Frame receive_login_frame(int fd) {
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
	CodeBattle::Login_Frame loginframe = BytesToLogin_Frame(data, len);
	return loginframe;
}

void send_total_frame(int fd, CodeBattle::Totalinfo_Frame totalinfoframe) {
	//send Totalinfo_Frame
	//Serialize
	unsigned int size = totalinfoframe.ByteSize();
	BYTE* bytes = new BYTE[size + 4];
	//Serialize len
	uint_to_bytes(size, bytes);
	//Serialize To Array
	totalinfoframe.SerializeToArray(bytes + 4, size);
	//Send msg
	int n = send(fd, bytes, size + 4, 0);
	if (n != (size + 4)) {
		fprintf(stderr, "invalid send len:%d, and should be:%d\n", n, size + 4);
	}
	delete[]bytes;
}

//receive first login frame
void receive_first_frame(int pre_fd, int later_fd) {
	//one player
	CodeBattle::Login_Frame loginframe = receive_login_frame(pre_fd);
	//choose ip client-side(for room environment)
	envir_choose_ip = loginframe.ip();
	//ip_username(the one)
	ip_username[envir_choose_ip] = loginframe.username();
	room_ip_store[0] = envir_choose_ip;
	char query_sql[100];
	//check user and pwd
	sprintf(query_sql, "select * from %s where username = '%s' and password = '%s'", user_table.c_str(), (loginframe.username()).c_str(), (loginframe.password()).c_str());
	if (!is_mysql_exist(query_sql)) {
		fprintf(stderr, "username:%s can't match mysql\n", (loginframe.username()).c_str());
		exit(1);
	}
	//send userinfo
	CodeBattle::Totalinfo_Frame totalinfoframe;
	sprintf(query_sql, "select * from %s where username = '%s'", user_info_table.c_str(), (loginframe.username()).c_str());
	CodeBattle::Userinfo_Frame userinfoframe = get_mysql_data(query_sql);
	userinfoframe.set_ip(loginframe.ip());

	CodeBattle::Userinfo_Frame* tmpinfo = totalinfoframe.mutable_preinfo();
	*tmpinfo = userinfoframe;
	//totalinfoframe.set_preinfo(userinfoframe);
	
	//another player
	loginframe = receive_login_frame(later_fd);
	//ip_username(another one)
	ip_username[loginframe.ip()] = loginframe.username();
	room_ip_store[1] = loginframe.ip();
	//check user and pwd
	sprintf(query_sql, "select * from %s where username = '%s' and password = '%s'", user_table.c_str(), (loginframe.username()).c_str(), (loginframe.password()).c_str());
	if (!is_mysql_exist(query_sql)) {
		fprintf(stderr, "username:%s can't match mysql\n", (loginframe.username()).c_str());
		exit(1);
	}
	//send userinfo
	sprintf(query_sql, "select * from %s where username = '%s'", user_info_table.c_str(), (loginframe.username()).c_str());
	userinfoframe = get_mysql_data(query_sql);
	userinfoframe.set_ip(loginframe.ip());
	//totalinfoframe.set_laterinfo(userinfoframe);
	tmpinfo = totalinfoframe.mutable_laterinfo();
	*tmpinfo = userinfoframe;

	//send totalinfoframe 
	send_total_frame(pre_fd, totalinfoframe);
	send_total_frame(later_fd, totalinfoframe);
}

//update finish
void update_finish_room() {
	//deal with lose
	string lose_name = ip_username[died_ip];
	if (lose_name == "") {
		fprintf(stderr, "died ip:%s map username is empty\n", died_ip.c_str());
		return;
	}
	cout << lose_name << endl;

	char query_sql[100];
	sprintf(query_sql, "select * from %s where username = '%s'", user_info_table.c_str(), lose_name.c_str());
	CodeBattle::Userinfo_Frame userinfoframe = get_mysql_data(query_sql);
	int winnumbers = userinfoframe.winnumbers();
	int totalnumbers = userinfoframe.losenumbers() + 1 + winnumbers;
	double winrate = (winnumbers*100.0) / totalnumbers;
	//update
	sprintf(query_sql, "update %s set losenumbers=%d,winrate=%d where username='%s'", user_info_table.c_str(), userinfoframe.losenumbers() + 1, (int)winrate, lose_name.c_str());
	updateSQL(query_sql);
	
	//deal with live
	string live_ip = "";
	if (room_ip_store[0] == died_ip) {
		live_ip = room_ip_store[1];
	}
	else {
		live_ip = room_ip_store[0];
	}
	string live_name = ip_username[live_ip];
	if (live_name == "") {
		fprintf(stderr, "live ip:%s map username is empty\n", live_ip.c_str());
		return;
	}
	cout << live_name << endl;

	sprintf(query_sql, "select * from %s where username = '%s'", user_info_table.c_str(), live_name.c_str());
	userinfoframe = get_mysql_data(query_sql);
	winnumbers = userinfoframe.winnumbers() + 1;
	totalnumbers = userinfoframe.losenumbers() + winnumbers;
	winrate = (winnumbers*100.0) / totalnumbers;
	//update
	sprintf(query_sql, "update %s set winnumbers=%d,winrate=%d where username='%s'", user_info_table.c_str(), winnumbers, (int)winrate, live_name.c_str());
	updateSQL(query_sql);
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
	//receive the first login package(when matched)
	receive_first_frame(pre_fd, later_fd);
	//reset rset
	FD_ZERO(&rset);
	struct timeval update_time, current_time;
	gettimeofday(&update_time, 0);
	while (1) {
		//receive queue
		Recv_Frame(pre_fd, later_fd);
		//
		gettimeofday(&current_time, 0);
		//float tmp_time = timedifference_msec(update_time, current_time);
		//printf("time:%f\n", tmp_time);
		if (timedifference_msec(update_time, current_time) >= LOGIC_FRAME) {
			//send queue
			Send_Frame(pre_fd, later_fd);
			gettimeofday(&update_time, 0);
		}
		//deal with finish
		if (is_died) {
			update_finish_room();
			sleep(5);
			close(pre_fd);
			close(later_fd);
			exit(0);
		}
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
	userinfoframe.set_ip(loginframe.ip());
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
	// 函数mysql_real_connect建立一个数据库连接  
	// 成功返回MYSQL*连接句柄，失败返回NULL  
	mysqlconn = mysql_real_connect(mysqlconn, host.c_str(),
		user.c_str(), pwd.c_str(), db_name.c_str(), 0, NULL, 0);
	if (mysqlconn == NULL)
	{
		mysql_with_error(mysqlconn, "mysql_real_connect failure");
	}
	printf("Connect Mysql successfully...\n");
}

void Init_mysql_connection() {
	mysqlconn = mysql_init(NULL); // 初始化数据库连接变量  
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
