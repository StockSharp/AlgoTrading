//+------------------------------------------------------------------+
//|                                                        Ticks.mq4 |
//|                                                       Lizhniyk E |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Lizhniyk E"
#property link      "http://www.metaquotes.net"

extern int volume=100;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int handle;
string file_name;
int init()
  {
//----
   file_name=Symbol()+" "+"volume "+volume+".csv";
   FileDelete(file_name);
   handle=FileOpen(file_name,FILE_CSV|FILE_WRITE,';'); 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
//                 open   high   low    close 
//2008.01.16,04:13,1.4817,1.4819,1.4817,1.4819,2
int count;
double h,l,o,c;
bool next=true;
string open_date, open_hour;
string data;
int start()
  {
//----
   count++;
   if(next) 
    {
    open_date=TimeToStr(TimeCurrent(),TIME_DATE);
    open_hour=TimeToStr(TimeCurrent(),TIME_MINUTES);
    o=Bid;
    h=Bid;
    l=Bid;
    next=false;
    }
   if(count!=volume)
    {
    if(Bid>h) h=Bid;
    if(Bid<l) l=Bid;
    }
    
   if(count==volume)
    {
    c=Bid;
    data=open_date+","+open_hour+","+o+","+h+","+l+","+c+","+count;
    count=0; //h=0; l=0;
    next=true;
    FileWrite(handle,data);
    }
//----
   return(0);
  }
//+------------------------------------------------------------------+

