//+------------------------------------------------------------------+
//|                                                           MC.mq4 |
//|                                                     space_cowboy |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "space_cowboy"
#property link      "http://www.metaquotes.net"

//---- input parameters
extern int start=21;//час начала работы
extern int end=9;//час окончания работы (плюс к начальному)
extern int period=36; //период для постороения канала в барах
extern double SLpp=300; //стоп в процентах от размаха канала

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int spread, stoplevel, freeze;
int handle;
int init()
  {
//----
  stoplevel=MarketInfo(Symbol(), MODE_STOPLEVEL)+1;
  spread=MarketInfo(Symbol(), MODE_SPREAD);
  freeze=MarketInfo(Symbol(), MODE_FREEZELEVEL)+1; 
  SLpp/=100;

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
int tmp, tc, st, et, dtmp;
int objs=0;
double high, low, raz;
bool op=false, work=false;
int start()
  {
//----
  tc=TimeCurrent(); 
  //***********************
  int d1bars=iBars(NULL,PERIOD_D1);
  if(tc>=et)
   { 
   if(dtmp!=d1bars)
    {
    int dow=0;
    if(start+end > 24) {dow=DayOfWeek();}
    int dt=iTime(NULL,PERIOD_D1,0);
    st=dt+start*3600;
    et=dt+(start+end)*3600;
    if(dow==5) et+=172800;
    }
   dtmp=d1bars; 
   }
   
  if(tc>=st && tc<=et) work=true; 
  else work=false;
  //***********************


   if(tmp!=Bars)
    {
    high=High[iHighest(NULL,0,MODE_HIGH,period,1)];
    low=Low[iLowest(NULL,0,MODE_LOW,period,1)];
    raz=((high-low)/Point)*SLpp;
    if(raz<stoplevel) raz=stoplevel;
    }
   tmp=Bars; 
   
   if(Bid>=high)
    {
    close_all(true);
    if(work && OrdersTotal()==0) sell();
    }
    
   if(Bid<=low)
    {
    close_all(false);
    if(work && OrdersTotal()==0) buy();
    } 
//----
   return(0);
  }
//+------------------------------------------------------------------+
int sell()
{
int t=-1;
t=OrderSend(Symbol(),OP_SELL,1,Bid,0,NormalizeDouble(Ask+raz*Point, Digits),0,"order_sell",29072007,0,0x0000FF);
return(t);
}

int buy()
{
int t=-1;
t=OrderSend(Symbol(),OP_BUY,1,Ask,0,NormalizeDouble(Bid-raz*Point, Digits),0,"order_buy",19072007,0,0xFF0000);
return(t);
}

void close_all(bool fl)
{
int tot=OrdersTotal();
for(int i=0;i<tot;i++)
 {
 OrderSelect(i,SELECT_BY_POS);
 if(OrderType()==0 && fl) OrderClose(OrderTicket(),OrderLots(),Bid,0);
 if(OrderType()==1 && !fl) OrderClose(OrderTicket(),OrderLots(),Ask,0);
 }
}

