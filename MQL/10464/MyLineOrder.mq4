//+------------------------------------------------------------------+
//|                                                  MyLineOrder.mq4 |
//|                                                 Chris Smallpeice |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Chris Smallpeice"
#include <stderror.mqh>
#include <stdlib.mqh>

extern  string LO_PREFIX="#";
extern  color  LO_ORDER_CLR=Gray;
extern  int    LO_ORDER_STYLE=STYLE_DASH;
extern  color  LO_STOPLOSS_CLR=Red;
extern  int    LO_STOPLOSS_STYLE=STYLE_DASHDOT;
extern  color  LO_TAKEPROFIT_CLR=Green;
extern  int    LO_TAKEPROFIT_STYLE=STYLE_DASHDOT;
extern  double LO_LOTS=0.1;
extern  double LO_PIPPROFIT=30;
extern  double LO_PIPSTOPLOSS=20;
extern  double LO_PIPTRAIL=0;
extern  bool   LO_ALARM=0;
extern  int MAGIC_NUMBER = 12943;
extern  bool   LO_ECN=0;
double Point.pip;

#define LO_KEY_S  0     // SL (pip)
#define LO_KEY_T  1     // TP (pip)
#define LO_KEY_SQ 2     // SL (quote)
#define LO_KEY_TQ 3     // TP (quote)
#define LO_KEY_LOT 4    // LOTSIZE
#define LO_KEY_TS  5    // TRALING STOP
#define LO_KEY_ALARM  6    // ALARM
#define LO_KEY_SIZE 7

#define LO_KEY_PRICE 7  // PRICE (NOT A KEY !!!!)
#define LO_KEY_PROCESSED 8 // HAS TRADE BEEN EXECUTED YET
#define LO_LINE_NAME 9 
#define LO_KEY_TYPE 10 
#define OL_ID 11
#define OL_PENDING 12
#define MLO_PREFIX "MLO"
#define OL_ORDER_BUFFER_SIZE 10
#define OL_SIZE 33

string LO_KEYS[]={"sl","tp","sq","tq","lo","ts","alarm"};
string line_name;

double orderInfo[OL_ORDER_BUFFER_SIZE][OL_SIZE];
string orderInfoDesc[OL_ORDER_BUFFER_SIZE];
double orderProp[OL_SIZE];
string orderDesc;
double MLO_LOTS;
double MLO_PIPPROFIT;
double MLO_PIPSTOPLOSS;
double MLO_PIPTRAIL,MLO_ALARM;
double STOP_LOSS,TAKE_PROFIT;
int COUNT_TRADES;
bool lock;
int count;

#define OL_ORDER_OPEN_CLR Red
#define OL_ORDER_CLOSE_CLR Green
#define OL_ORDER_MODIFY_CLR Violet

/*
   Possible updates
   
   OCO (Order cancels other)
   Multiple SL, TP
   To change SL and TP at set levels
   Line Alarms(non-trade)
   Local pending orders
*/
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
MLO_LOTS = LO_LOTS;
MLO_PIPPROFIT = LO_PIPPROFIT;
MLO_PIPSTOPLOSS = LO_PIPSTOPLOSS;
MLO_PIPTRAIL = LO_PIPTRAIL;
MLO_ALARM = LO_ALARM;
lock=false;
if (Digits == 5 || Digits == 3){    // Adjust for five (5) digit brokers.
Point.pip = Point*10;
}else{
Point.pip = Point;
}
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
int start()
  {
//----
if(count<5)count++;else{
Print("Working");
checkLines();
UpdateLines(); 
cleanUpLines();  
count=0;

}
//----
   return(0);
  }
//+------------------------------------------------------------------+
void checkLines()
{
   bool newtrade = false;
   string name="";
   int type;
   if(ObjectGet(LO_PREFIX+"buy",OBJPROP_PRICE1)>0&&!lock)
   {
   name = LO_PREFIX+"buy";
   type = OP_BUY;
   newtrade=true;
   }else if(ObjectGet(LO_PREFIX+"buypend",OBJPROP_PRICE1)>0&&!lock)
   {
   name = LO_PREFIX+"buypend";
   if(ObjectGet(LO_PREFIX+"buypend",OBJPROP_PRICE1)>Ask){
    type = OP_BUYSTOP;
    }else {
    type = OP_BUYLIMIT;
    }
    orderInfo[COUNT_TRADES][OL_PENDING]=1;
    newtrade=true;
   }else if(ObjectGet(LO_PREFIX+"sell",OBJPROP_PRICE1)>0&&!lock)
   {
   name = LO_PREFIX+"sell";
   type = OP_SELL;
   newtrade=true;
   }else if(ObjectGet(LO_PREFIX+"sellpend",OBJPROP_PRICE1)>0&&!lock)
   {
   name = LO_PREFIX+"sellpend";
   if(ObjectGet(LO_PREFIX+"sellpend",OBJPROP_PRICE1)<Ask){
    type = OP_SELLSTOP;
    }else {
    type = OP_SELLLIMIT;
    }
    orderInfo[COUNT_TRADES][OL_PENDING]=1;
    newtrade=true;
   }
   
   if(newtrade)
   {
      orderInfo[COUNT_TRADES][LO_KEY_PRICE]=ObjectGet(name,OBJPROP_PRICE1);
      orderInfo[COUNT_TRADES][LO_KEY_TYPE]= type;
      line_name=name;
   orderDesc=ObjectDescription(name)+" ";
   orderInfoDesc[COUNT_TRADES] = ObjectDescription(name);
 
   int inx_start=0;
   int inx_stop=0;
   for(int t=0;t<LO_KEY_SIZE-1;t++)
   {
      inx_start=StringFind(orderDesc,LO_KEYS[t]+"=",0);    
      if(inx_start==-1) continue;
      else
      {
         inx_start=StringLen(LO_KEYS[t]+"=")+inx_start;
         inx_stop=StringFind(orderDesc," ",inx_start);
         if(inx_stop==-1) continue;
         else
         {
            orderInfo[COUNT_TRADES][t]=NormalizeDouble(StrToDouble(StringSubstr(orderDesc,inx_start,inx_stop-inx_start)),Digits+1);
         }
      }
   }
   orderInfo[COUNT_TRADES][LO_KEY_PROCESSED] = 0;
   if(IsTradeAllowed())    OrderProcess();
   }
MLO_LOTS = LO_LOTS;
MLO_PIPPROFIT = LO_PIPPROFIT;
MLO_PIPSTOPLOSS = LO_PIPSTOPLOSS;
MLO_PIPTRAIL = LO_PIPTRAIL;

}

int OrderProcess()
{
for(int i=0;i<COUNT_TRADES+1;i++)
   {
   double type;
   STOP_LOSS=0;
   TAKE_PROFIT=0;
   if(orderInfo[i][LO_KEY_PROCESSED]==0&&orderInfo[i][LO_KEY_PRICE]>0)
   {
   int EXPIRE=0;
  
   int ordertype=StrToInteger(DoubleToStr(orderInfo[i][LO_KEY_TYPE],0));
   if(orderInfo[i][LO_KEY_LOT]!=0)MLO_LOTS = orderInfo[i][LO_KEY_LOT];else orderInfo[i][LO_KEY_LOT] = MLO_LOTS;
   if(orderInfo[i][LO_KEY_T]!=0)MLO_PIPPROFIT = orderInfo[i][LO_KEY_T];else orderInfo[i][LO_KEY_T] = MLO_PIPPROFIT;
   if(orderInfo[i][LO_KEY_S]!=0)MLO_PIPSTOPLOSS = orderInfo[i][LO_KEY_S];else orderInfo[i][LO_KEY_S] = MLO_PIPSTOPLOSS;
   if(orderInfo[i][LO_KEY_ALARM]!=0)MLO_ALARM = orderInfo[i][LO_KEY_ALARM];else orderInfo[i][LO_KEY_ALARM] = MLO_ALARM;
   if(orderInfo[i][LO_KEY_TS]==0)
   if(ordertype==OP_BUY||ordertype==OP_BUYLIMIT||ordertype==OP_BUYSTOP){
   if(orderInfo[i][OL_PENDING]==1){
   STOP_LOSS=orderInfo[i][LO_KEY_PRICE]-(MLO_PIPSTOPLOSS*Point.pip);
   TAKE_PROFIT=orderInfo[i][LO_KEY_PRICE]+(MLO_PIPPROFIT*Point.pip);
   type = NormalizeDouble(orderInfo[i][LO_KEY_PRICE],Digits);
   }else{
   STOP_LOSS=Bid-(MLO_PIPSTOPLOSS*Point.pip);
   TAKE_PROFIT=Bid+(MLO_PIPPROFIT*Point.pip);
   type=Ask;
   }
   
   }else if(ordertype==OP_SELL||ordertype==OP_SELLLIMIT||ordertype==OP_SELLSTOP){
   if(orderInfo[i][OL_PENDING]==1){
  STOP_LOSS=NormalizeDouble(orderInfo[i][LO_KEY_PRICE]+(MLO_PIPSTOPLOSS*Point.pip),Digits);
   TAKE_PROFIT=NormalizeDouble(orderInfo[i][LO_KEY_PRICE]-(MLO_PIPPROFIT*Point.pip),Digits);
   type = NormalizeDouble(orderInfo[i][LO_KEY_PRICE],Digits);
   }else{
   STOP_LOSS=Ask+(MLO_PIPSTOPLOSS*Point.pip);
   TAKE_PROFIT=Ask-(MLO_PIPPROFIT*Point.pip);
   type=Bid;
   }
   
   }
   orderInfo[i][LO_KEY_SQ]=STOP_LOSS;
   orderInfo[i][LO_KEY_TQ]=TAKE_PROFIT;
   if(LO_ECN==1) int ticket = OrderSend(Symbol(),ordertype,MLO_LOTS,type,5,0,0,"",MAGIC_NUMBER,EXPIRE);else ticket = OrderSend(Symbol(),ordertype,MLO_LOTS,type,5,STOP_LOSS,TAKE_PROFIT,"",MAGIC_NUMBER,EXPIRE);
   if(ticket<0)Alert("Trade not gone through "+ErrorDescription(GetLastError()));else{
   orderInfo[i][OL_ID] = ticket;
   if(LO_ECN==1){
   OrderSelect(ticket,SELECT_BY_TICKET);
   OrderModify(ticket,OrderOpenPrice(),STOP_LOSS,TAKE_PROFIT,0);
   }
   if(ordertype==OP_BUY){
   if(orderInfo[i][OL_PENDING]==1){
   ObjectCreate(LO_PREFIX+ticket+"",OBJ_HLINE,0,0,orderInfo[i][LO_KEY_PRICE]);
   }else{
   ObjectCreate(LO_PREFIX+ticket+"",OBJ_HLINE,0,0,Ask);
   }}else if(ordertype==OP_SELL)
   {
   if(orderInfo[i][OL_PENDING]==1){
   ObjectCreate(LO_PREFIX+ticket,OBJ_HLINE,0,0,orderInfo[i][LO_KEY_PRICE]);
   }else{
   ObjectCreate(LO_PREFIX+ticket,OBJ_HLINE,0,0,Bid);
   }}
   string text = StringConcatenate("sl=",orderInfo[i][LO_KEY_S]," tp=",orderInfo[i][LO_KEY_T]," ts="+DoubleToStr(orderInfo[i][LO_KEY_TS],1)," lo=",orderInfo[i][LO_KEY_LOT]," alarm=",orderInfo[i][LO_KEY_ALARM]);
   ObjectSetText(LO_PREFIX+ticket,text,2);
   ObjectCreate(LO_PREFIX+ticket+" SL",OBJ_HLINE,0,0,0);
   ObjectCreate(LO_PREFIX+ticket+" TP",OBJ_HLINE,0,0,0);
   ObjectSet(LO_PREFIX+ticket,OBJPROP_COLOR,LO_ORDER_CLR);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_COLOR,LO_STOPLOSS_CLR);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_PRICE1,orderInfo[i][LO_KEY_SQ]);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_PRICE1,orderInfo[i][LO_KEY_TQ]);
   ObjectSet(LO_PREFIX+ticket,OBJPROP_STYLE,LO_ORDER_STYLE);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
   ObjectDelete(line_name);
   orderInfo[i][LO_KEY_PROCESSED]=1;
   
   COUNT_TRADES++;
   }
   }
   }
}

void UpdateLines()
{

for(int i=0;i<OrdersTotal();i++)
{
bool update=false;
double LOTS,STOP_LOSS_PIP,TAKE_PROFIT_PIP,TS;
OrderSelect(i, SELECT_BY_POS,MODE_TRADES);
LOTS = OrderLots();
STOP_LOSS = OrderStopLoss();
TAKE_PROFIT = OrderTakeProfit();
string str;
if(ObjectFind(LO_PREFIX+OrderTicket())>-1){
//Main line exists

if(OrderType()==OP_BUY||OrderType()==OP_SELL){
//Order open
ObjectSet(LO_PREFIX+OrderTicket(),OBJPROP_PRICE1,OrderOpenPrice());
if(lock==false)int handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);else handle = 0;

if(handle>0)
    {
    lock = true;
     str=FileReadString(handle,FileSize(handle));
     if(str!=ObjectDescription(LO_PREFIX+OrderTicket())){

     str = ObjectDescription(LO_PREFIX+OrderTicket());
          FileClose(handle);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);          
     FileWriteString(handle, str,StringLen(str));
     FileFlush(handle);
          FileClose(handle);
          lock=false;
int inx_start=StringFind(str,"sl=",0);    
         inx_start=StringLen("sl=")+inx_start;
    int      inx_stop=StringFind(str," ",inx_start);  
STOP_LOSS_PIP = NormalizeDouble(StrToDouble(StringSubstr(str,inx_start,inx_stop-inx_start)),Digits+1);   
if(OrderType()==OP_BUY)STOP_LOSS=OrderOpenPrice()-(STOP_LOSS_PIP*Point.pip);
if(OrderType()==OP_SELL)STOP_LOSS=(STOP_LOSS_PIP*Point.pip)+OrderOpenPrice();
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);

 inx_start=StringFind(str,"tp=",0);    
         inx_start=StringLen("tp=")+inx_start;
          inx_stop=StringFind(str," ",inx_start);  
TAKE_PROFIT_PIP = NormalizeDouble(StrToDouble(StringSubstr(str,inx_start,inx_stop-inx_start)),Digits+1);   
if(OrderType()==OP_BUY)TAKE_PROFIT=OrderOpenPrice()+(TAKE_PROFIT_PIP*Point.pip);
if(OrderType()==OP_SELL)TAKE_PROFIT=OrderOpenPrice()-(TAKE_PROFIT_PIP*Point.pip);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,TAKE_PROFIT);
    }
    }
str = ObjectDescription(LO_PREFIX+OrderTicket())+" ";
 inx_start=StringFind(str,"ts=",0);    
         inx_start=StringLen("ts=")+inx_start;
          inx_stop=StringFind(str," ",inx_start); 
 TS = NormalizeDouble(StrToDouble(StringSubstr(str,inx_start,inx_stop-inx_start)),1);

 inx_start=StringFind(str,"sl=",0);    
         inx_start=StringLen("sl=")+inx_start;
          inx_stop=StringFind(str," ",inx_start);  
STOP_LOSS_PIP = NormalizeDouble(StrToDouble(StringSubstr(str,inx_start,inx_stop-inx_start)),1); 

if(TS>0){

double PipProfit = OrderClosePrice()-OrderOpenPrice();

if(OrderType()==OP_SELL)PipProfit = OrderOpenPrice()-OrderClosePrice();
if(PipProfit>TS*Point.pip)
{
STOP_LOSS_PIP = OrderClosePrice()-OrderStopLoss();
if(OrderType()==OP_SELL)STOP_LOSS_PIP = OrderStopLoss()-OrderClosePrice();
if(STOP_LOSS_PIP>TS*Point.pip)
{

if(OrderType()==OP_BUY&&(OrderClosePrice()-(TS*Point.pip))>OrderStopLoss()){
STOP_LOSS = OrderClosePrice()-(TS*Point.pip);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);
}
if(OrderType()==OP_SELL&&(OrderClosePrice()+(TS*Point.pip))<OrderStopLoss()){
STOP_LOSS = OrderClosePrice()+(TS*Point.pip);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);
}
}
}
}
//if(OrderStopLoss()>0){
if(ObjectFind(LO_PREFIX+OrderTicket()+" SL")>-1){
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderStopLoss(),Digits)){
update=true;
STOP_LOSS = ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1);
if(OrderType()==OP_BUY)STOP_LOSS_PIP=NormalizeDouble((OrderOpenPrice()-STOP_LOSS)/Point.pip,1);
if(OrderType()==OP_SELL)STOP_LOSS_PIP=NormalizeDouble((STOP_LOSS-OrderOpenPrice())/Point.pip,1);
}else{
if(OrderType()==OP_BUY)STOP_LOSS_PIP=NormalizeDouble((OrderOpenPrice()-OrderStopLoss())/Point.pip,1);
if(OrderType()==OP_SELL)STOP_LOSS_PIP=NormalizeDouble((OrderStopLoss()-OrderOpenPrice())/Point.pip,1);
}
}else{
ObjectCreate(LO_PREFIX+OrderTicket()+" SL",OBJ_HLINE,0,0,0);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_COLOR,LO_STOPLOSS_CLR);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,OrderStopLoss());
}
/*}else{

}*/
//if(OrderTakeProfit()>0){
if(ObjectFind(LO_PREFIX+OrderTicket()+" TP")>-1){
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderTakeProfit(),Digits)){
update=true;
TAKE_PROFIT = ObjectGet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1);
if(OrderType()==OP_SELL)TAKE_PROFIT_PIP=NormalizeDouble((OrderOpenPrice()-TAKE_PROFIT)/Point.pip,1);
if(OrderType()==OP_BUY)TAKE_PROFIT_PIP=NormalizeDouble((TAKE_PROFIT-OrderOpenPrice())/Point.pip,1);
}else{
if(OrderType()==OP_SELL)TAKE_PROFIT_PIP=NormalizeDouble((OrderOpenPrice()-OrderTakeProfit())/Point.pip,1);
if(OrderType()==OP_BUY)TAKE_PROFIT_PIP=NormalizeDouble((OrderTakeProfit()-OrderOpenPrice())/Point.pip,1);
}
}else{
ObjectCreate(LO_PREFIX+OrderTicket()+" TP",OBJ_HLINE,0,0,0);

ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,OrderTakeProfit());
}
/*}else{

}*/

if(update==true)
{
OrderModify(OrderTicket(),OrderOpenPrice(),STOP_LOSS,TAKE_PROFIT,0);
str = ObjectDescription(LO_PREFIX+OrderTicket())+" ";
 inx_start=StringFind(str,"alarm=",0);    
 if(inx_start>=0){
         inx_start=StringLen("alarm=")+inx_start;
          inx_stop=StringFind(str," ",inx_start); 
 double alarm = NormalizeDouble(StrToDouble(StringSubstr(str,inx_start,inx_stop-inx_start)),0);
 }else alarm = LO_ALARM;
ObjectSetText(LO_PREFIX+OrderTicket(),StringConcatenate("sl=",STOP_LOSS_PIP," tp=",TAKE_PROFIT_PIP," lo=",LOTS," ts=",TS," alarm=",alarm),2);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
if(handle>0)
    {
     str=FileReadString(handle,FileSize(handle)-1);
     if(str!=ObjectDescription(LO_PREFIX+OrderTicket())){

     str = ObjectDescription(LO_PREFIX+OrderTicket());
     FileWriteString(handle, str,StringLen(str));
     }
     FileClose(handle);
    }
}
}else{
//Pending order
double PEND_PRICE = NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket(),OBJPROP_PRICE1),Digits);
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket(),OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderOpenPrice(),Digits)){
update=true;
}
if(ObjectFind(LO_PREFIX+OrderTicket()+" SL")>-1){
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderStopLoss(),Digits)){
update=true;
if((ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1)<Ask && (OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP))||(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1)>Bid && (OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)))ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,OrderStopLoss());
STOP_LOSS = NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1),Digits);
if(OrderType()==OP_BUY)STOP_LOSS_PIP=NormalizeDouble((OrderOpenPrice()-STOP_LOSS)/Point.pip,1);
if(OrderType()==OP_SELL)STOP_LOSS_PIP=NormalizeDouble((STOP_LOSS-OrderOpenPrice())/Point.pip,1);
}else{
if(OrderType()==OP_BUY)STOP_LOSS_PIP=NormalizeDouble((OrderOpenPrice()-OrderStopLoss())/Point.pip,1);
if(OrderType()==OP_SELL)STOP_LOSS_PIP=NormalizeDouble((OrderStopLoss()-OrderOpenPrice())/Point.pip,1);
}
}else{
ObjectCreate(LO_PREFIX+OrderTicket()+" SL",OBJ_HLINE,0,0,0);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_COLOR,LO_STOPLOSS_CLR);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,OrderStopLoss());
}

if(ObjectFind(LO_PREFIX+OrderTicket()+" TP")>-1){
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderTakeProfit(),Digits)){
update=true;
TAKE_PROFIT = NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1),Digits);
if(OrderType()==OP_SELL)TAKE_PROFIT_PIP=NormalizeDouble((OrderOpenPrice()-TAKE_PROFIT)/Point.pip,1);
if(OrderType()==OP_BUY)TAKE_PROFIT_PIP=NormalizeDouble((TAKE_PROFIT-OrderOpenPrice())/Point.pip,1);
}else{
if(OrderType()==OP_SELL)TAKE_PROFIT_PIP=NormalizeDouble((OrderOpenPrice()-OrderTakeProfit())/Point.pip,1);
if(OrderType()==OP_BUY)TAKE_PROFIT_PIP=NormalizeDouble((OrderTakeProfit()-OrderOpenPrice())/Point.pip,1);
}
}else{
ObjectCreate(LO_PREFIX+OrderTicket()+" TP",OBJ_HLINE,0,0,0);

ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,OrderTakeProfit());
}
if(update==true)
{
OrderModify(OrderTicket(),PEND_PRICE,STOP_LOSS,TAKE_PROFIT,0);
}

}
}else{
//Main line doesn't exists

int ticket = OrderTicket();
ObjectCreate(LO_PREFIX+ticket,OBJ_HLINE,0,0,OrderOpenPrice());
ObjectSet(LO_PREFIX+ticket,OBJPROP_COLOR,LO_ORDER_CLR);
ObjectSet(LO_PREFIX+ticket,OBJPROP_STYLE,LO_ORDER_STYLE);
   string text = StringConcatenate("sl=",MLO_PIPSTOPLOSS," tp=",MLO_PIPPROFIT," ts="+DoubleToStr(MLO_PIPTRAIL,1)," lo=",MLO_LOTS);
   ObjectSetText(LO_PREFIX+ticket,text,2);
   ObjectCreate(LO_PREFIX+ticket+" SL",OBJ_HLINE,0,0,0);
   ObjectCreate(LO_PREFIX+ticket+" TP",OBJ_HLINE,0,0,0);
   ObjectSet(LO_PREFIX+ticket,OBJPROP_COLOR,LO_ORDER_CLR);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_COLOR,LO_STOPLOSS_CLR);
   if(OrderStopLoss()==0){
   STOP_LOSS = OrderOpenPrice()-(MLO_PIPSTOPLOSS*Point.pip);
   if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)STOP_LOSS = OrderOpenPrice()+(MLO_PIPSTOPLOSS*Point.pip);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_PRICE1,STOP_LOSS);
   }else ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_PRICE1,OrderStopLoss());
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
   if(OrderTakeProfit()==0){
   TAKE_PROFIT = OrderOpenPrice()+(MLO_PIPPROFIT*Point.pip);
   if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)TAKE_PROFIT = OrderOpenPrice()-(MLO_PIPPROFIT*Point.pip);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_PRICE1,TAKE_PROFIT);
   }else ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_PRICE1,OrderTakeProfit());
   ObjectSet(LO_PREFIX+ticket,OBJPROP_STYLE,LO_ORDER_STYLE);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
}

//End of for loop
}
//End of function
}
void cleanUpLines()
{
for(int i=0;i<ObjectsTotal();i++)
{
if(ObjectType(ObjectName(i))==OBJ_HLINE){
string name = ObjectName(i);
if(StringFind(name,LO_PREFIX)>-1){
double text = StrToDouble(StringSubstr(name,1,StringFind(name," ",StringLen(LO_PREFIX))));
if(OrderSelect(text,SELECT_BY_TICKET)==true){
if(OrderCloseTime()>0){
if(ObjectFind(LO_PREFIX+text)>-1){
string str = ObjectDescription(LO_PREFIX+text)+" ";
  int inx_start=StringFind(str,"alarm=",0);    
      inx_start=StringLen("alarm=")+inx_start;
         int inx_stop=StringFind(str," ",inx_start); 
 double alarm = NormalizeDouble(StrToDouble(StringSubstr(str,inx_start,inx_stop-inx_start)),1);
 if(alarm==1)Alert("The trade this line was following has closed.");
}
ObjectDelete(name);

}
}else{
//ObjectDelete(name);
}

}}}
}

