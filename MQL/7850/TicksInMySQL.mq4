//+------------------------------------------------------------------+
//|                                                 TicksInMySQL.mq4 |
//|                      Copyright © 2006, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

#import "libmysql.dll"
int mysql_init(int db);
int mysql_errno(int TMYSQL);
int mysql_real_connect(int TMYSQL, string host, string user, string password, 
                       string DB,int port,int socket,int clientflag);
int mysql_real_query(int TMSQL, string query, int length);
void mysql_close(int TMSQL);
#import
int mysql;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   mysql = mysql_init(mysql);
   if(mysql != 0) 
       Print("allocated");
   string host = "localhost";
   string user = "user";
   string password = "pwd";
   string DB = "mt4";
   int clientflag = 0;
   int port = 3306;
   string socket = "";
   int res = mysql_real_connect(mysql,host,user,password,DB,port,socket,clientflag);
   int err = GetLastError();
   if(res == mysql) 
       Print("connected");
   else 
       Print("error=", mysql, " ", mysql_errno(mysql), " ");
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
   mysql_close(mysql);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   string query = "";
   int length = 0;
   query = StringConcatenate("insert into ticks(margin,freemargin,date,ask,bid,symbol,equity) values(",
                             AccountMargin(), ",", AccountFreeMargin(), ",\"", 
                             TimeToStr(CurTime(), TIME_DATE|TIME_SECONDS), "\",", 
                             NormalizeDouble(Ask, 4), ",", NormalizeDouble(Bid, 4),
                             ",\"", Symbol(), "\",", AccountEquity(), ");");
   length = StringLen(query);
   mysql_real_query(mysql, query, length);
   int myerr = mysql_errno(mysql);
   if(myerr > 0)
       Print("error=",myerr);
  }
//+------------------------------------------------------------------+

