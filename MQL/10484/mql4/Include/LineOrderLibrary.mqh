//+------------------------------------------------------------------+
//|                                             LineOrderLibrary.mq4 |
//|                                                            Chris |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Chris"
#property link      ""
#include <stderror.mqh>
#include <stdlib.mqh>
#include <WinUser32.mqh>

extern  string LO_PREFIX="#"; // Name of lines = LO_PREFIX+TicketNumber()+Specialty
extern  double LO_LOTS=0.1;
extern  double LO_PIPPROFIT=30;
extern  double LO_PIPSTOPLOSS=20;
extern  double LO_PIPTRAIL=0; // This trail acts like the default MT4 trail, once you are in profit by this much then the trail will start
extern  bool   LO_AUTO_INCLUDE_SL_TP = 1; // If no values entered then default values used
extern  bool   LO_CLOSE_ORDER_ON_DELETE = 1;  // Close order on deleting the main line else will re-create line next time
extern  int    LO_ALARM=0; // 0 = No alarm, 1 = Alert, 2 = Email(Not implemented yet), 3 = Send file(Not implemented)
extern  bool   LO_ECN=0; // Is the broker a ECN?
extern  int    MAGIC_NUMBER = -1;  // Set at -1 to apply to all currently open trades
extern  color  LO_ORDER_CLR=Gray; // Colour of open price line
extern  int    LO_ORDER_STYLE=STYLE_DASH; // Style of open price line
extern  color  LO_STOPLOSS_CLR=Red; // Colour of order's stop loss
extern  int    LO_STOPLOSS_STYLE=STYLE_DASHDOT; // Style of order's stop loss
extern  color  LO_MOVE_STOPLOSS_CLR=Teal; // Colour of line which moves stoploss a specified stoploss when hit
extern  int    LO_MOVE_STOPLOSS_STYLE=STYLE_DASHDOT; // Style of line which moves stoploss a specified stoploss when hit
extern  color  LO_STOPLOSS_MOVE_CLR=Orange; // Colour of line to which to move stop loss to
extern  int    LO_STOPLOSS_MOVE_STYLE=STYLE_DASHDOT; // Style of line to which to move stop loss to
extern  color  LO_STOPLOSS_CLOSE_CLR=Red; // The colour of line which closes at a stop loss
extern  int    LO_STOPLOSS_CLOSE_STYLE=STYLE_DASHDOT; // The style of line which closes at a stop loss
extern  color  LO_TAKEPROFIT_CLR=Green; // Colour of the final take profit
extern  int    LO_TAKEPROFIT_STYLE=STYLE_DASHDOT; // Style of line of final take profit
extern  color  LO_TAKEPROFIT_MOVE_CLR=Green; // Colour of the move take profit
extern  int    LO_TAKEPROFIT_MOVE_STYLE=STYLE_DASHDOT; // Style of the move take profit
extern  color  LO_TAKEPROFIT_CLOSE_CLR=Green; // Colour of the close take profit 
extern  int    LO_TAKEPROFIT_CLOSE_STYLE=STYLE_DASHDOT; // Style of the close take profit

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
string LO_NESSESARY_KEYS[]={"sl","tp","lo","ts"};
string line_name;

double orderInfo[OL_SIZE];
string orderInfoDesc[OL_ORDER_BUFFER_SIZE];
double orderProp[OL_SIZE];
string orderDesc;
double MLO_LOTS,MLO_PIPPROFIT,MLO_PIPSTOPLOSS,MLO_PIPTRAIL,MLO_ALARM,STOP_LOSS,TAKE_PROFIT;
string MLO_LOTS_STRING,MLO_PIPPROFIT_STRING,MLO_PIPSTOPLOSS_STRING,MLO_PIPTRAIL_STRING,STOP_LOSS_STRING,TAKE_PROFIT_STRING;
int COUNT_TRADES;
bool lock;
int count;

#define OL_ORDER_OPEN_CLR Red
#define OL_ORDER_CLOSE_CLR Green
#define OL_ORDER_MODIFY_CLR Violet

void initVar()
{
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
}

void  deinitVar(){
     switch(UninitializeReason())
       {
        case REASON_CHARTCLOSE:
        case REASON_REMOVE:  
            for(int i=0;i<ObjectsTotal();i++)
{
if(ObjectType(ObjectName(i))==OBJ_HLINE){
string name = ObjectName(i);
if(StringFind(name,LO_PREFIX)>-1){
double text = StrToDouble(StringSubstr(name,1,StringFind(name," ",StringLen(LO_PREFIX))));
if(OrderSelect(text,SELECT_BY_TICKET)==true){


string str = ObjectDescription(ObjectName(i))+" ";
if(name == LO_PREFIX+OrderTicket()){
 FileDelete(MLO_PREFIX+OrderTicket()+".txt");
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" SL-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" SL-Count");
 }
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" TP-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" TP-Count");
 }
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" LO-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" LO-Count");
 }
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" TS-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" TS-Count");
 }
 }
}
}
ObjectDelete(name);
}}
            break; // cleaning up and deallocation of all resources.
        case REASON_RECOMPILE:
        case REASON_CHARTCHANGE:
        case REASON_PARAMETERS:
        case REASON_ACCOUNT: 
        break;      
       }

  }

void processLines(){
checkLines();
UpdateLines(); 
cleanUpLines(); 

}

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
    orderInfo[OL_PENDING]=1;
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
    orderInfo[OL_PENDING]=1;
    newtrade=true;
   }
   
   if(newtrade)
   {
      orderInfo[LO_KEY_PRICE]=ObjectGet(name,OBJPROP_PRICE1);
      orderInfo[LO_KEY_TYPE]= type;
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
           orderInfo[t]=0;
           if(StringSubstr(orderDesc,inx_start,inx_stop-inx_start)!="N")orderInfo[t]=NormalizeDouble(StrToDouble(StringSubstr(orderDesc,inx_start,inx_stop-inx_start)),Digits+1);else orderInfo[t]=42;
         }
      }
   }
   orderInfo[LO_KEY_PROCESSED] = 0;
      COUNT_TRADES++;
   if(IsTradeAllowed())    OrderProcess();
   }
initVar();

}

int OrderProcess()
{
//COUNT_TRADES = 1;

   double type;
   STOP_LOSS=0;
   TAKE_PROFIT=0;
   if(orderInfo[LO_KEY_PROCESSED]==0&&orderInfo[LO_KEY_PRICE]>0)
   {
   int EXPIRE=0;
  
   int ordertype=StrToInteger(DoubleToStr(orderInfo[LO_KEY_TYPE],0));
   if(orderInfo[LO_KEY_LOT]!=0)/*{
   if(orderInfo[LO_KEY_LOT]!=42)*/MLO_LOTS = orderInfo[LO_KEY_LOT];
   /*else MLO_LOTS = 0;}*/else orderInfo[LO_KEY_LOT] = MLO_LOTS;
   if(orderInfo[LO_KEY_T]!=0){
   if(orderInfo[LO_KEY_T]!=42)MLO_PIPPROFIT = orderInfo[LO_KEY_T];else{
   MLO_PIPPROFIT=0;
   orderInfo[LO_KEY_TQ] = 42;
   }}else if(LO_AUTO_INCLUDE_SL_TP==1)orderInfo[LO_KEY_T] = MLO_PIPPROFIT;else {
   orderInfo[LO_KEY_T]=0;
   orderInfo[LO_KEY_TQ]=42;
   }
   if(orderInfo[LO_KEY_S]!=0){
   if(orderInfo[LO_KEY_S]!=42)MLO_PIPSTOPLOSS = orderInfo[LO_KEY_S];else{
   MLO_PIPSTOPLOSS = 0;
   orderInfo[LO_KEY_SQ] = 42;
   }}else if(LO_AUTO_INCLUDE_SL_TP==1)orderInfo[LO_KEY_S] = MLO_PIPSTOPLOSS;else {
   orderInfo[LO_KEY_S]=0;
   orderInfo[LO_KEY_SQ]=42;
   }
   if(orderInfo[LO_KEY_ALARM]!=0)MLO_ALARM = orderInfo[LO_KEY_ALARM];else orderInfo[LO_KEY_ALARM] = MLO_ALARM;
   if(orderInfo[LO_KEY_TS]==0)
   if(ordertype==OP_BUY||ordertype==OP_BUYLIMIT||ordertype==OP_BUYSTOP){
   if(orderInfo[OL_PENDING]==1){
   STOP_LOSS=orderInfo[LO_KEY_PRICE]-(MLO_PIPSTOPLOSS*Point.pip);
   TAKE_PROFIT=orderInfo[LO_KEY_PRICE]+(MLO_PIPPROFIT*Point.pip);
   type = NormalizeDouble(orderInfo[LO_KEY_PRICE],Digits);
   }else{
   STOP_LOSS=Bid-(MLO_PIPSTOPLOSS*Point.pip);
   TAKE_PROFIT=Bid+(MLO_PIPPROFIT*Point.pip);
   type=Ask;
   }
   if(orderInfo[LO_KEY_SQ]==42)STOP_LOSS = 0;
   if(orderInfo[LO_KEY_TQ]==42)TAKE_PROFIT = 0;
   }else if(ordertype==OP_SELL||ordertype==OP_SELLLIMIT||ordertype==OP_SELLSTOP){
   if(orderInfo[OL_PENDING]==1){
  STOP_LOSS=NormalizeDouble(orderInfo[LO_KEY_PRICE]+(MLO_PIPSTOPLOSS*Point.pip),Digits);
   TAKE_PROFIT=NormalizeDouble(orderInfo[LO_KEY_PRICE]-(MLO_PIPPROFIT*Point.pip),Digits);
   type = NormalizeDouble(orderInfo[LO_KEY_PRICE],Digits);
   }else{
   STOP_LOSS=Ask+(MLO_PIPSTOPLOSS*Point.pip);
   TAKE_PROFIT=Ask-(MLO_PIPPROFIT*Point.pip);
   type=Bid;
   }
   if(orderInfo[LO_KEY_SQ]==42)STOP_LOSS = 0;
   if(orderInfo[LO_KEY_TQ]==42)TAKE_PROFIT = 0;
   }
   
   orderInfo[LO_KEY_SQ]=STOP_LOSS;
   orderInfo[LO_KEY_TQ]=TAKE_PROFIT;
   int SEND_MAGIC_NUMBER=MAGIC_NUMBER;
   if(SEND_MAGIC_NUMBER<0)SEND_MAGIC_NUMBER=0;
   if(LO_ECN==1) int ticket = OrderSend(Symbol(),ordertype,MLO_LOTS,type,10,0,0,"",SEND_MAGIC_NUMBER,EXPIRE);else ticket = OrderSend(Symbol(),ordertype,MLO_LOTS,type,10,STOP_LOSS,TAKE_PROFIT,"",MAGIC_NUMBER,EXPIRE);
   if(ticket<0){
   int error =GetLastError();
   if(error==130){

   Alert("The lines are to close to the market price or the open price, will set the lines to minimum distance + spread");
   if(ordertype==OP_BUY||ordertype==OP_SELL){
   if(ordertype==OP_BUY){ 
   if(MarketInfo(Symbol(),MODE_STOPLEVEL)*Point>(type-STOP_LOSS))STOP_LOSS = type-(MarketInfo(Symbol(),MODE_STOPLEVEL)+MarketInfo(Symbol(),MODE_SPREAD))*Point;
   if(MarketInfo(Symbol(),MODE_STOPLEVEL)*Point>(TAKE_PROFIT-type))TAKE_PROFIT = type+(MarketInfo(Symbol(),MODE_STOPLEVEL)+MarketInfo(Symbol(),MODE_SPREAD))*Point;
   }else{
   if(MarketInfo(Symbol(),MODE_STOPLEVEL)*Point>(STOP_LOSS-type))STOP_LOSS = type+(MarketInfo(Symbol(),MODE_STOPLEVEL)+MarketInfo(Symbol(),MODE_SPREAD))*Point;
   if(MarketInfo(Symbol(),MODE_STOPLEVEL)*Point>(type-TAKE_PROFIT))TAKE_PROFIT = type-(MarketInfo(Symbol(),MODE_STOPLEVEL)+MarketInfo(Symbol(),MODE_SPREAD))*Point;
   }
   }else{
   switch(ordertype)
   {
   case OP_BUYLIMIT:
   
   
   break;
   
   case OP_BUYSTOP:
   
   
   break;
   
   case OP_SELLLIMIT:
   
   
   break;
   
   case OP_SELLSTOP:
   
   
   break;
   
   }
   }
   if(LO_ECN==1) ticket = OrderSend(Symbol(),ordertype,MLO_LOTS,type,5,0,0,"",SEND_MAGIC_NUMBER,EXPIRE);else ticket = OrderSend(Symbol(),ordertype,MLO_LOTS,type,5,STOP_LOSS,TAKE_PROFIT,"",MAGIC_NUMBER,EXPIRE);
   if(ticket<0)Alert("Sorry it failed again "+ErrorDescription(GetLastError()));
   


   }else{
    
   if(error==129){
   // Invalid prices
   return(0);
   }else{
   Alert("Trade not gone through "+error+" "+ErrorDescription(error)+" SL:"+STOP_LOSS+" Open price:"+type+" Ask: "+Ask+" Bid:"+Bid+" TP:"+TAKE_PROFIT+" ");
   }    
   }
   }//else{
   orderInfo[OL_ID] = ticket;
   if(LO_ECN==1){
   OrderSelect(ticket,SELECT_BY_TICKET);
   OrderModify(ticket,OrderOpenPrice(),STOP_LOSS,TAKE_PROFIT,0);
   }
   if(ordertype==OP_BUY){
   if(orderInfo[OL_PENDING]==1){
   ObjectCreate(LO_PREFIX+ticket+" pend",OBJ_HLINE,0,0,orderInfo[LO_KEY_PRICE]);
   }else{
   ObjectCreate(LO_PREFIX+ticket+"",OBJ_HLINE,0,0,Ask);
   }}else if(ordertype==OP_SELL)
   {
   if(orderInfo[OL_PENDING]==1){
   ObjectCreate(LO_PREFIX+ticket+" pend",OBJ_HLINE,0,0,orderInfo[LO_KEY_PRICE]);
   }else{
   ObjectCreate(LO_PREFIX+ticket,OBJ_HLINE,0,0,Bid);
   }}
   string alarm_string ="";
string str = ObjectDescription(line_name)+" ";
return_value(str,"alarm=",alarm_string,"b"," alarm=");
string SL_string = "";
return_value(str,"sl=",SL_string);
if(SL_string=="")if(LO_AUTO_INCLUDE_SL_TP==0)SL_string="N";else SL_string = DoubleToStr(orderInfo[LO_KEY_S],1);
string TP_string = "";
return_value(str,"tp=",TP_string);
if(TP_string=="")if(LO_AUTO_INCLUDE_SL_TP==0)TP_string="N";else TP_string = DoubleToStr(orderInfo[LO_KEY_T],1);
   string text = StringConcatenate("sl=",SL_string," tp=",TP_string," ts="+DoubleToStr(orderInfo[LO_KEY_TS],1)," lo=",orderInfo[LO_KEY_LOT],alarm_string);
   ObjectSetText(LO_PREFIX+ticket,text,2);
   ObjectCreate(LO_PREFIX+ticket+" SL",OBJ_HLINE,0,0,0);
   ObjectCreate(LO_PREFIX+ticket+" TP",OBJ_HLINE,0,0,0);
   ObjectSet(LO_PREFIX+ticket,OBJPROP_COLOR,LO_ORDER_CLR);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_COLOR,LO_STOPLOSS_CLR);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_PRICE1,orderInfo[LO_KEY_SQ]);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_PRICE1,orderInfo[LO_KEY_TQ]);
   ObjectSet(LO_PREFIX+ticket,OBJPROP_STYLE,LO_ORDER_STYLE);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
   if(!ObjectDelete(line_name))Alert(line_name);
   orderInfo[LO_KEY_PROCESSED]=1;
   
   COUNT_TRADES--;
   }
initVar();
   }

int UpdateMultipleLines(int ticket)
{

string LOTS_STRING_ARRAY[][2],STOP_LOSS_PIP_STRING_ARRAY[][2],TAKE_PROFIT_PIP_STRING_ARRAY[][2],TS_STRING_ARRAY[][2];
OrderSelect(ticket, SELECT_BY_TICKET);
if(MAGIC_NUMBER>=0){
if(OrderMagicNumber()!=MAGIC_NUMBER)return (0);
}
string extra = "";
if(OrderType()!=OP_BUY&&OrderType()!=OP_SELL)extra=" pend";
string str = ObjectDescription(LO_PREFIX+OrderTicket()+extra);
string STOP_LOSS_PIP_STRING = "";
return_value(str,"sl=",STOP_LOSS_PIP_STRING,"b"," sl=");
string TAKE_PROFIT_PIP_STRING = "";
return_value(str,"tp=",TAKE_PROFIT_PIP_STRING,"b"," tp=");
string STOP_LOSS_QUOTE_STRING = "";
return_value(str,"sq=",STOP_LOSS_PIP_STRING,"b"," sq=");
string TAKE_PROFIT_QUOTE_STRING = "";
return_value(str,"tq=",TAKE_PROFIT_PIP_STRING,"b"," tq=");
string TRAIL_STOP_PIP_STRING = "";
return_value(str,"ts=",TRAIL_STOP_PIP_STRING,"b"," ts=");
string LOT_ORDER_STRING = "";
return_value(str,"lo=",LOT_ORDER_STRING,"b"," lo=");
string alarm_string = "";
return_value(str,"alarm=",alarm_string,"b"," alarm=");

int SL_order_count=0;
int TP_order_count=0;
int LO_order_count=0;
int TS_order_count=0;
Explode(STOP_LOSS_PIP_STRING,",",STOP_LOSS_PIP_STRING_ARRAY,1);
Explode(TAKE_PROFIT_PIP_STRING,",",TAKE_PROFIT_PIP_STRING_ARRAY,1);
Explode(LOT_ORDER_STRING,",",LOTS_STRING_ARRAY,1);
Explode(TRAIL_STOP_PIP_STRING,",",TS_STRING_ARRAY,1);

processMulitLine(OrderTicket(),str, STOP_LOSS_PIP_STRING_ARRAY,STOP_LOSS_PIP_STRING,StringConcatenate(LO_PREFIX,ticket," SL")," SL-Count","sl=",LO_STOPLOSS_MOVE_CLR,LO_STOPLOSS_MOVE_STYLE);
processMulitLine(OrderTicket(),str, TAKE_PROFIT_PIP_STRING_ARRAY,TAKE_PROFIT_PIP_STRING,StringConcatenate(LO_PREFIX,ticket," TP")," TP-Count","tp=",LO_TAKEPROFIT_MOVE_CLR,LO_TAKEPROFIT_MOVE_STYLE);
processMulitLine(OrderTicket(),str, LOTS_STRING_ARRAY,LOT_ORDER_STRING,StringConcatenate(LO_PREFIX,ticket," LO")," LO-Count","lo=",Yellow,STYLE_DASHDOT);
processMulitLine(OrderTicket(),str, TS_STRING_ARRAY,TRAIL_STOP_PIP_STRING,StringConcatenate(LO_PREFIX,ticket," TS")," TS-Count","ts=",Yellow,STYLE_DASHDOT);
ObjectSetText(LO_PREFIX+OrderTicket()+extra,STOP_LOSS_PIP_STRING+STOP_LOSS_QUOTE_STRING+TAKE_PROFIT_PIP_STRING+TAKE_PROFIT_QUOTE_STRING+TRAIL_STOP_PIP_STRING+LOT_ORDER_STRING+alarm_string);
return (0);
}

int UpdateLines()
{
int error = GetLastError();
for(int i=0;i<OrdersTotal();i++)
{
bool update=false;
double LOTS,TS=LO_PIPTRAIL;
string LOTS_STRING,STOP_LOSS_PIP_STRING,TAKE_PROFIT_PIP_STRING,TS_STRING,STOP_LOSS_PIP,TAKE_PROFIT_PIP;
string LOTS_STRING_ARRAY[][2],STOP_LOSS_PIP_STRING_ARRAY[][2],TAKE_PROFIT_PIP_STRING_ARRAY[][2],TS_STRING_ARRAY[][2];
OrderSelect(i, SELECT_BY_POS,MODE_TRADES);
if(OrderSymbol()!=Symbol())continue;
if(MAGIC_NUMBER>=0){
if(OrderMagicNumber()!=MAGIC_NUMBER)return (0);
}
UpdateMultipleLines(OrderTicket());

LOTS = OrderLots();
STOP_LOSS = OrderStopLoss();
TAKE_PROFIT = OrderTakeProfit();

string str="";
string extra = "";
if(OrderType()!=OP_BUY&&OrderType()!=OP_SELL)extra=" pend";
if(ObjectFind(LO_PREFIX+OrderTicket()+extra)>-1){
//Main line exists

if(OrderType()==OP_BUY||OrderType()==OP_SELL){
//Order open
if(ObjectFind(LO_PREFIX+OrderTicket()+" pend")>-1){
ObjectCreate(LO_PREFIX+OrderTicket(),OBJ_HLINE,0,0,OrderOpenPrice());
ObjectSetText(LO_PREFIX+OrderTicket(),ObjectDescription(LO_PREFIX+OrderTicket()+extra));
ObjectDelete(LO_PREFIX+OrderTicket()+" pend");
}

ObjectSet(LO_PREFIX+OrderTicket(),OBJPROP_PRICE1,OrderOpenPrice());
str = ObjectDescription(LO_PREFIX+OrderTicket())+" ";

if(lock==false){
int handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
if(handle==-1){
FileClose(handle);
handle = -1;

handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);
FileClose(handle);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
}
}else handle = 0;
if(handle>0)
    {
    lock = true;
     str=FileReadString(handle,FileSize(handle));
     if(str!=ObjectDescription(LO_PREFIX+OrderTicket())){
     
     str = ObjectDescription(LO_PREFIX+OrderTicket());
          FileClose(handle);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);          
     FileWriteString(handle, str,StringLen(str));
          FileClose(handle);
              
int SL_order_count=0;
int TP_order_count=0;
int LO_order_count=0;
int TS_order_count=0;
string store="";
return_value(str,"sl=",STOP_LOSS_PIP_STRING);
Explode(STOP_LOSS_PIP_STRING,",",STOP_LOSS_PIP_STRING_ARRAY,1);

for(i=0;i<ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0);i++){
Explode(STOP_LOSS_PIP_STRING_ARRAY[i][0],"@", STOP_LOSS_PIP_STRING_ARRAY,2,i);
if(STOP_LOSS_PIP_STRING_ARRAY[i][1]!=""){
SL_order_count++;

}else STOP_LOSS_PIP = STOP_LOSS_PIP_STRING_ARRAY[i][0];
}
if(SL_order_count==ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0)&&SL_order_count!=0)STOP_LOSS_PIP = LO_PIPSTOPLOSS;else if(SL_order_count==ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0))STOP_LOSS_PIP = "N";

if(OrderType()==OP_BUY&&STOP_LOSS_PIP!="N")STOP_LOSS=OrderOpenPrice()-(StrToDouble(STOP_LOSS_PIP)*Point.pip);else if(OrderType()==OP_BUY)STOP_LOSS = 0;
if(OrderType()==OP_SELL&&STOP_LOSS_PIP!="N")STOP_LOSS=(StrToDouble(STOP_LOSS_PIP)*Point.pip)+OrderOpenPrice();else if(OrderType()==OP_SELL)STOP_LOSS = 0;
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);
return_value(str,"tp=",TAKE_PROFIT_PIP_STRING);
Explode(TAKE_PROFIT_PIP_STRING,",",TAKE_PROFIT_PIP_STRING_ARRAY,1);
    
for(i=0;i<ArrayRange(TAKE_PROFIT_PIP_STRING_ARRAY,0);i++){
Explode(TAKE_PROFIT_PIP_STRING_ARRAY[i][0],"@", TAKE_PROFIT_PIP_STRING_ARRAY,2,i);

if(TAKE_PROFIT_PIP_STRING_ARRAY[i][1]!=""){
TP_order_count++;

}else TAKE_PROFIT_PIP = TAKE_PROFIT_PIP_STRING_ARRAY[i][0];
}
if(TP_order_count==ArrayRange(TAKE_PROFIT_PIP_STRING_ARRAY,0)&&TP_order_count!=0)TAKE_PROFIT_PIP = LO_PIPPROFIT; 
if(OrderType()==OP_BUY&&TAKE_PROFIT_PIP!="N")TAKE_PROFIT=OrderOpenPrice()+(StrToDouble(TAKE_PROFIT_PIP)*Point.pip);else if(OrderType()==OP_BUY)TAKE_PROFIT=0;
if(OrderType()==OP_SELL&&TAKE_PROFIT_PIP!="N")TAKE_PROFIT=OrderOpenPrice()-(StrToDouble(TAKE_PROFIT_PIP)*Point.pip);else if(OrderType()==OP_SELL)TAKE_PROFIT=0;
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,TAKE_PROFIT);
return_value(str,"ts=",TS_STRING);
Explode(TS_STRING,",",TS_STRING_ARRAY,1);
for(i=0;i<ArrayRange(TS_STRING_ARRAY,0);i++){
Explode(TS_STRING_ARRAY[i][0],"@", TS_STRING_ARRAY,2,i);
if(TS_STRING_ARRAY[i][1]!=""){
TS_order_count++;
}else TS = NormalizeDouble(StrToDouble(TS_STRING_ARRAY[i][0]),1);

}
if(TS_order_count==ArrayRange(TS_STRING_ARRAY,0)&&TS_order_count!=0)TS = LO_PIPTRAIL; 
//End of difference in file vs description
    }else{
    TS = StrToDouble(TS_STRING_ARRAY[0][0]);
    }
    lock=false;
FileClose(handle);
    }
    
if(TS>0){
double PipProfit = OrderClosePrice()-OrderOpenPrice();
if(OrderType()==OP_SELL)PipProfit = OrderOpenPrice()-OrderClosePrice();
if(PipProfit>TS*Point.pip)
{
double StopLoss = OrderClosePrice()-OrderStopLoss();
if(OrderType()==OP_SELL)StopLoss = OrderStopLoss()-OrderClosePrice();
if(StopLoss>TS*Point.pip)
{
if(OrderType()==OP_BUY&&(OrderClosePrice()-(TS*Point.pip))>OrderStopLoss()){
STOP_LOSS = OrderClosePrice()-(TS*Point.pip);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);
}
if(OrderType()==OP_SELL&&(OrderClosePrice()+(TS*Point.pip))<OrderStopLoss()){
STOP_LOSS = OrderClosePrice()+(TS*Point.pip);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);
}}}}

if(ObjectFind(LO_PREFIX+OrderTicket()+" SL")>-1){
STOP_LOSS = ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1);
if(STOP_LOSS == 0){
STOP_LOSS_PIP="N";
store = ObjectDescription(LO_PREFIX+OrderTicket()+" SL");
if(store==""){
if(LO_AUTO_INCLUDE_SL_TP==1)if(MessageBox("Your stop loss for order "+OrderTicket()+store+" is at 0. Would you like to set it at "+DoubleToStr(LO_PIPSTOPLOSS,1)+" pips now?","Invalid Stop loss",MB_YESNO)==IDYES){
if(OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP){
STOP_LOSS = OrderOpenPrice()-LO_PIPSTOPLOSS*Point.pip;
}else{
STOP_LOSS = LO_PIPSTOPLOSS*Point.pip-OrderOpenPrice();
}
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);

}else{
ObjectSetText(LO_PREFIX+OrderTicket()+" SL","keepzero=1",2);
}
}}

if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderStopLoss(),Digits)){
update=true;
if(OrderType()==OP_BUY)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-STOP_LOSS)/Point.pip,1),1);
if(OrderType()==OP_SELL)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((STOP_LOSS-OrderOpenPrice())/Point.pip,1),1);
}else{
if(OrderType()==OP_BUY)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-OrderStopLoss())/Point.pip,1),1);
if(OrderType()==OP_SELL)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((OrderStopLoss()-OrderOpenPrice())/Point.pip,1),1);
}
if(STOP_LOSS==0)STOP_LOSS_PIP="N";
}else{
ObjectCreate(LO_PREFIX+OrderTicket()+" SL",OBJ_HLINE,0,0,0);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_COLOR,LO_STOPLOSS_CLR);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,OrderStopLoss());
}
if(ObjectFind(LO_PREFIX+OrderTicket()+" TP")>-1){
TAKE_PROFIT = ObjectGet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1);

if(TAKE_PROFIT == 0){
store = ObjectDescription(LO_PREFIX+OrderTicket()+" TP");
if(store==""){
if(LO_AUTO_INCLUDE_SL_TP==1)if(MessageBox("Your take profit for order "+OrderTicket()+" is at 0. Would you like to set it at "+DoubleToStr(MLO_PIPPROFIT,1)+" pips now?","Invalid Take profit",MB_YESNO)==IDYES){
if(OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP){
TAKE_PROFIT = MLO_PIPPROFIT*Point.pip+OrderOpenPrice();
}else{
TAKE_PROFIT = OrderOpenPrice()-MLO_PIPPROFIT*Point.pip;
}
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,TAKE_PROFIT);
}else{
ObjectSetText(LO_PREFIX+OrderTicket()+" TP","keepzero=1",2);
}
}}
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderTakeProfit(),Digits)){
update=true;
if(OrderType()==OP_SELL)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-TAKE_PROFIT)/Point.pip,1),1);
if(OrderType()==OP_BUY)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((TAKE_PROFIT-OrderOpenPrice())/Point.pip,1),1);
}else{
if(OrderType()==OP_SELL)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-OrderTakeProfit())/Point.pip,1),1);
if(OrderType()==OP_BUY)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((OrderTakeProfit()-OrderOpenPrice())/Point.pip,1),1);
}

}else{
ObjectCreate(LO_PREFIX+OrderTicket()+" TP",OBJ_HLINE,0,0,0);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,OrderTakeProfit());
}
if(TAKE_PROFIT==0)TAKE_PROFIT_PIP="N";
if(update==true)
{
if(!OrderModify(OrderTicket(),OrderOpenPrice(),STOP_LOSS,TAKE_PROFIT,0))
{
Alert("The modification has gone wrong. SL = "+DoubleToStr(STOP_LOSS,Digits)+" TP = "+DoubleToStr(TAKE_PROFIT,Digits));
STOP_LOSS = OrderStopLoss();
TAKE_PROFIT = OrderTakeProfit();
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,OrderStopLoss());
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,OrderTakeProfit());
}
string alarm_string ="";
str = ObjectDescription(LO_PREFIX+OrderTicket())+" ";
return_value(str,"alarm=",alarm_string,"b"," alarm=");

string STOP_LOSS_QUOTE_STRING = "";
return_value(str,"sq=",STOP_LOSS_QUOTE_STRING,"b"," sq=");
string TAKE_PROFIT_QUOTE_STRING = "";
return_value(str,"tq=",TAKE_PROFIT_QUOTE_STRING,"b"," tq=");
       LOTS_STRING = DoubleToStr(LOTS,1);
return_value(str,"lo=",LOTS_STRING);

 STOP_LOSS_PIP_STRING = STOP_LOSS_PIP;
for(i=0;i<ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0);i++){
if(STOP_LOSS_PIP_STRING_ARRAY[i][1]!=""){
STOP_LOSS_PIP_STRING = StringConcatenate(STOP_LOSS_PIP_STRING,","+STOP_LOSS_PIP_STRING_ARRAY[i][0]+"@"+STOP_LOSS_PIP_STRING_ARRAY[i][1]);
}
}

 TAKE_PROFIT_PIP_STRING = TAKE_PROFIT_PIP;
for(i=0;i<ArrayRange(TAKE_PROFIT_PIP_STRING_ARRAY,0);i++){
if(TAKE_PROFIT_PIP_STRING_ARRAY[i][1]!=""){
TAKE_PROFIT_PIP_STRING = StringConcatenate(TAKE_PROFIT_PIP_STRING,","+TAKE_PROFIT_PIP_STRING_ARRAY[i][0]+"@"+TAKE_PROFIT_PIP_STRING_ARRAY[i][1]);
}
}

 TS_STRING = DoubleToStr(TS,1);
for(i=0;i<ArrayRange(TS_STRING_ARRAY,0);i++){
if(TS_STRING_ARRAY[i][1]!=""){
TS_STRING = StringConcatenate(TS_STRING,","+TS_STRING_ARRAY[i][0]+"@"+TS_STRING_ARRAY[i][1]);
}
}
ObjectSetText(LO_PREFIX+OrderTicket(),StringConcatenate("sl=",STOP_LOSS_PIP_STRING,STOP_LOSS_QUOTE_STRING," tp=",TAKE_PROFIT_PIP_STRING,TAKE_PROFIT_QUOTE_STRING," lo=",LOTS_STRING," ts=",TS_STRING,alarm_string),2);
str = ObjectDescription(LO_PREFIX+OrderTicket())+" ";
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ|FILE_WRITE);
if(handle>0)
    {
     str=FileReadString(handle,FileSize(handle)-1);
     if(str!=ObjectDescription(LO_PREFIX+OrderTicket())){
     str = ObjectDescription(LO_PREFIX+OrderTicket());
     FileWriteString(handle, str,StringLen(str));
     }}
    FileClose(handle);
}
}else{
//Pending order
double PEND_PRICE = NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" pend",OBJPROP_PRICE1),Digits);
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" pend",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderOpenPrice(),Digits)){
update=true;
}
str=ObjectDescription(LO_PREFIX+OrderTicket()+" pend");

if(lock==false){
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
if(handle==-1){
FileClose(handle);
handle = -1;

handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);
FileClose(handle);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
}
}else handle = 0;

if(handle>0)
    {
    lock = true;
     str=FileReadString(handle,FileSize(handle));
     if(str!=ObjectDescription(LO_PREFIX+OrderTicket())){
     
     str = ObjectDescription(LO_PREFIX+OrderTicket());
          FileClose(handle);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);          
     FileWriteString(handle, str,StringLen(str));
          FileClose(handle);
}}              
 SL_order_count=0;
 TP_order_count=0;
 LO_order_count=0;
 TS_order_count=0;
 store="";

return_value(str,"sl=",STOP_LOSS_PIP_STRING);
Explode(STOP_LOSS_PIP_STRING,",",STOP_LOSS_PIP_STRING_ARRAY,1);

for(i=0;i<ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0);i++){
Explode(STOP_LOSS_PIP_STRING_ARRAY[i][0],"@", STOP_LOSS_PIP_STRING_ARRAY,2,i);
if(STOP_LOSS_PIP_STRING_ARRAY[i][1]!=""){
SL_order_count++;

}else STOP_LOSS_PIP = STOP_LOSS_PIP_STRING_ARRAY[i][0];
}
if(SL_order_count==ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0)&&SL_order_count!=0)STOP_LOSS_PIP = LO_PIPSTOPLOSS;else if(SL_order_count==ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0))STOP_LOSS_PIP = "N";



if(ObjectFind(LO_PREFIX+OrderTicket()+" SL")>-1){
if(NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderStopLoss(),Digits)){
update=true;
if((ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1)<Ask && (OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP))||(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1)>Bid && (OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)))ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,OrderStopLoss());
STOP_LOSS = NormalizeDouble(ObjectGet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1),Digits);
if(OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-STOP_LOSS)/Point.pip,1),1);
if(OrderType()==OP_SELLSTOP||OrderType()==OP_SELLLIMIT)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((STOP_LOSS-OrderOpenPrice())/Point.pip,1),1);
}else{
if(OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-OrderStopLoss())/Point.pip,1),1);
if(OrderType()==OP_SELLSTOP||OrderType()==OP_SELLLIMIT)STOP_LOSS_PIP=DoubleToStr(NormalizeDouble((OrderStopLoss()-OrderOpenPrice())/Point.pip,1),1);
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
if(OrderType()==OP_SELLSTOP||OrderType()==OP_SELLLIMIT)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-TAKE_PROFIT)/Point.pip,1),1);
if(OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((TAKE_PROFIT-OrderOpenPrice())/Point.pip,1),1);
}else{
if(OrderType()==OP_SELLSTOP||OrderType()==OP_SELLLIMIT)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((OrderOpenPrice()-OrderTakeProfit())/Point.pip,1),1);
if(OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)TAKE_PROFIT_PIP=DoubleToStr(NormalizeDouble((OrderTakeProfit()-OrderOpenPrice())/Point.pip,1),1);
}
}else{
ObjectCreate(LO_PREFIX+OrderTicket()+" TP",OBJ_HLINE,0,0,0);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,OrderTakeProfit());
}

if(update==true)
{
if(OrderModify(OrderTicket(),PEND_PRICE,STOP_LOSS,TAKE_PROFIT,0)==false)Alert(ErrorDescription(GetLastError()));
str = ObjectDescription(LO_PREFIX+OrderTicket())+" ";
alarm_string ="";
  STOP_LOSS_QUOTE_STRING = "";
return_value(str,"sq=",STOP_LOSS_QUOTE_STRING,"b"," sq=");
  TAKE_PROFIT_QUOTE_STRING = "";
return_value(str,"tq=",TAKE_PROFIT_QUOTE_STRING,"b"," tq=");
       LOTS_STRING = DoubleToStr(LOTS,1);
return_value(str,"lo=",LOTS_STRING);

 STOP_LOSS_PIP_STRING = STOP_LOSS_PIP;
for(i=0;i<ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0);i++){
if(STOP_LOSS_PIP_STRING_ARRAY[i][1]!=""){
STOP_LOSS_PIP_STRING = StringConcatenate(STOP_LOSS_PIP_STRING,","+STOP_LOSS_PIP_STRING_ARRAY[i][0]+"@"+STOP_LOSS_PIP_STRING_ARRAY[i][1]);
}
}

 TAKE_PROFIT_PIP_STRING = TAKE_PROFIT_PIP;
for(i=0;i<ArrayRange(TAKE_PROFIT_PIP_STRING_ARRAY,0);i++){
if(TAKE_PROFIT_PIP_STRING_ARRAY[i][1]!=""){
TAKE_PROFIT_PIP_STRING = StringConcatenate(TAKE_PROFIT_PIP_STRING,","+TAKE_PROFIT_PIP_STRING_ARRAY[i][0]+"@"+TAKE_PROFIT_PIP_STRING_ARRAY[i][1]);
}
}

 TS_STRING = DoubleToStr(TS,1);
for(i=0;i<ArrayRange(TS_STRING_ARRAY,0);i++){
if(TS_STRING_ARRAY[i][1]!=""){
TS_STRING = StringConcatenate(TS_STRING,","+TS_STRING_ARRAY[i][0]+"@"+TS_STRING_ARRAY[i][1]);
}
}

ObjectSetText(LO_PREFIX+OrderTicket()+" pend",StringConcatenate("sl=",STOP_LOSS_PIP_STRING," tp=",TAKE_PROFIT_PIP_STRING," lo=",LOTS," ts=",TS_STRING,alarm_string),2);
str = ObjectDescription(LO_PREFIX+OrderTicket())+" ";
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ|FILE_WRITE);
if(handle>0)
    {
     str=FileReadString(handle,FileSize(handle)-1);
     if(str!=ObjectDescription(LO_PREFIX+OrderTicket())){
     str = ObjectDescription(LO_PREFIX+OrderTicket());
     FileWriteString(handle, str,StringLen(str));
     }}
    FileClose(handle);
    }

}
}else{
//Main line doesn't exists
int ticket = OrderTicket();
if(LO_CLOSE_ORDER_ON_DELETE==1){
 if(TimeCurrent()-OrderOpenTime()>30){
 if(MessageBox("You are sure you want to close order "+OrderTicket()+"? ","Closing order "+OrderTicket(),MB_YESNO)==IDYES){
 if(OrderType()==OP_BUY||OrderType()==OP_SELL)OrderClose(ticket,OrderLots(),OrderClosePrice(),10);else OrderDelete(ticket);
return (0);
}else{

}}

}
extra = "";
if(OrderType()!=OP_BUY&&OrderType()!=OP_SELL)extra=" pend";
ObjectCreate(LO_PREFIX+ticket+extra,OBJ_HLINE,0,0,OrderOpenPrice());
ObjectSet(LO_PREFIX+ticket+extra,OBJPROP_COLOR,LO_ORDER_CLR);
ObjectSet(LO_PREFIX+ticket+extra,OBJPROP_STYLE,LO_ORDER_STYLE);
STOP_LOSS = OrderStopLoss();
TAKE_PROFIT = OrderTakeProfit();
if((OrderType()==OP_BUY||OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)&&OrderStopLoss()!=0){
STOP_LOSS_PIP = DoubleToStr(NormalizeDouble((OrderOpenPrice()-STOP_LOSS)/Point.pip,1),1);
TAKE_PROFIT_PIP = DoubleToStr(NormalizeDouble((TAKE_PROFIT-OrderOpenPrice())/Point.pip,1),1);
}else{
TAKE_PROFIT_PIP = DoubleToStr(NormalizeDouble((OrderOpenPrice()-TAKE_PROFIT)/Point.pip,1),1);
STOP_LOSS_PIP = DoubleToStr(NormalizeDouble((STOP_LOSS-OrderOpenPrice())/Point.pip,1),1);
}
   string text = StringConcatenate("sl="+STOP_LOSS_PIP," tp=",TAKE_PROFIT_PIP," ts="+DoubleToStr(MLO_PIPTRAIL,1)," lo=",OrderLots());
   if(extra==""&&ObjectFind(LO_PREFIX+ticket+" pend")>-1){
   text = ObjectDescription(LO_PREFIX+ticket+" pend")+" ";
    int inx_start=StringFind(text,"alarm=",0);    
 if(inx_start>=0){
         inx_start=StringLen("alarm=")+inx_start;
        int   inx_stop=StringFind(text," ",inx_start);   
          int alarm = NormalizeDouble(StrToDouble(StringSubstr(text,inx_start,inx_stop-inx_start)),Digits+1);
          Alert("Your pending order has been filled. "+alarm);         
 }
   ObjectSetText(LO_PREFIX+ticket+extra,ObjectDescription(LO_PREFIX+ticket+" pend"),2);
   ObjectDelete(LO_PREFIX+ticket+" pend");
   }else ObjectSetText(LO_PREFIX+ticket+extra,text,2);
   if(ObjectFind(LO_PREFIX+ticket+" SL")==-1){
   ObjectCreate(LO_PREFIX+ticket+" SL",OBJ_HLINE,0,0,0);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_COLOR,LO_STOPLOSS_CLR);
   ObjectSet(LO_PREFIX+ticket+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
   }
   if(ObjectFind(LO_PREFIX+ticket+" TP")==-1){
   ObjectCreate(LO_PREFIX+ticket+" TP",OBJ_HLINE,0,0,0);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_CLR);
   ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
   }
   ObjectSet(LO_PREFIX+ticket+extra,OBJPROP_COLOR,LO_ORDER_CLR);
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
   }else ObjectSet(LO_PREFIX+ticket+" TP",OBJPROP_PRICE1,OrderTakeProfit());  
   ObjectSet(LO_PREFIX+ticket,OBJPROP_STYLE,LO_ORDER_STYLE);
}
//End of for loop
}
//End of function
return (0);
}
void cleanUpLines()
{
for(int i=0;i<ObjectsTotal();i++)
{
if(ObjectType(ObjectName(i))==OBJ_HLINE){
string name = ObjectName(i);
if(StringFind(name,LO_PREFIX)>-1){
double text = StrToDouble(StringSubstr(name,1,StringFind(name," ",StringLen(LO_PREFIX))));
if(text==-1)ObjectDelete(name);
if(OrderSelect(text,SELECT_BY_TICKET)==true){
if(OrderCloseTime()>0){

string str = ObjectDescription(ObjectName(i))+" ";
  int inx_start=StringFind(str,"alarm=",0);    

if(name == LO_PREFIX+OrderTicket()){
  if(inx_start>=0)
  {
      inx_start=StringLen("alarm=")+inx_start;
         int inx_stop=StringFind(str," ",inx_start); 
 int alarm = StrToInteger(StringSubstr(str,inx_start,inx_stop-inx_start));
 if(alarm==1){
 if(OrderStopLoss()==OrderClosePrice()) Alert("The trade reached it\'s stop loss");else if(OrderTakeProfit()==OrderClosePrice()) Alert("The trade reached it\'s take profit");else Alert("The trade was closed manually");
 }
}
 FileDelete(MLO_PREFIX+OrderTicket()+".txt");
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" SL-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" SL-Count");
 }
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" TP-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" TP-Count");
 }
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" LO-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" LO-Count");
 }
 if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" TS-Count")){
 GlobalVariableDel(MLO_PREFIX+OrderTicket()+" TS-Count");
 }
 
 }
ObjectDelete(name);
}
}
}}}
}
int Count(string str, string delimiter)
{
  int i = 0;
  int pos = StringFind(str, delimiter);
while(pos != -1)
   {
      i++;
      str = StringSubstr(str, pos+StringLen(delimiter));
      pos = StringFind(str, delimiter);
      if(pos == -1) break;
   }
   return(i+1);
}
int Explode(string str, string delimiter, string& arr[][], int dimension, int number=0)
{

   int i = 0;
   int pos = StringFind(str, delimiter);
if(dimension == 1) ArrayResize(arr,Count(str,delimiter));
   while(pos != -1)
   {
   if(dimension == 1){
      if(pos == 0) arr[i][number] = ""; else arr[i][number] = StringSubstr(str, 0, pos);
      arr[i][1]="";
}else
{
      if(pos == 0) arr[number][i] = ""; else arr[number][i] = StringSubstr(str, 0, pos);
}
      i++;
      str = StringSubstr(str, pos+StringLen(delimiter));
      pos = StringFind(str, delimiter);
      if(pos == -1 || str == "") break;
   }
   if(dimension == 1){
    arr[i][number] = str;
    arr[i][1] = "";
   }else arr[number][i] = str;
   return(i+1);
}

string return_value(string body, string search, string& variable, string position="n", string addtion="")
{
 int inx_start=StringFind(body,search,0);    
 if(inx_start>=0){
         inx_start=StringLen(search)+inx_start;
     int inx_stop=StringFind(body," ",inx_start); 
 if(position=="n"){
 variable = StringSubstr(body,inx_start,inx_stop-inx_start);
 }else if(position=="b"){
 variable = StringConcatenate(addtion,StringSubstr(body,inx_start,inx_stop-inx_start));
 }else if(position=="a"){
 variable = StringConcatenate(StringSubstr(body,inx_start,inx_stop-inx_start),addtion);
 }}
}

void processMulitLine(int ticket, string str, string& VAR_STRING_ARRAY[][],string& VAR_STRING,string lineName,string globalname, string description,color LineColour,int LineStyle){
int count;
OrderSelect(ticket,SELECT_BY_TICKET);
if(!GlobalVariableCheck(MLO_PREFIX+OrderTicket()+globalname))GlobalVariableSet(MLO_PREFIX+OrderTicket()+globalname,0);
for(int o=0;o<ArrayRange(VAR_STRING_ARRAY,0);o++)
{
Explode(VAR_STRING_ARRAY[o][0],"@", VAR_STRING_ARRAY,2,o);
if(VAR_STRING_ARRAY[o][1]!=""){
count++;
double value = StrToDouble(VAR_STRING_ARRAY[o][1]);
if(value==0){

}else {
if(ObjectFind(lineName+count)>-1){
double SL_Level = ObjectGet(lineName+count,OBJPROP_PRICE1);
if(NormalizeDouble(SL_Level,Digits)!=value){
VAR_STRING_ARRAY[o][1]=DoubleToStr(SL_Level,Digits);
if(StrToDouble(VAR_STRING_ARRAY[o][1])<Bid)ObjectSetText(lineName+count,"dir=b");else ObjectSetText(lineName+count,"dir=a");
}
str = ObjectDescription(lineName+count);
string store;
return_value(str,"dir=",store);
if((store == "a"&&Bid>SL_Level)||(store== "b"&& Ask<SL_Level)){
if(OrderType()==OP_BUY||OrderType()==OP_SELL){
VAR_STRING_ARRAY[0][0]=description+VAR_STRING_ARRAY[o][0];
VAR_STRING_ARRAY[o][1]= "";
ObjectDelete(lineName+count);
}
}
//Line does exist
}else{
ObjectCreate(lineName+count,OBJ_HLINE,0,0,StrToDouble(VAR_STRING_ARRAY[o][1]));

ObjectSet(lineName+count,OBJPROP_COLOR,LineColour);
if(StrToDouble(VAR_STRING_ARRAY[o][1])<Bid)ObjectSetText(lineName+count,"dir=b");else ObjectSetText(lineName+count,"dir=a");
//Line doesn't exist
}
//End of second value being successfully converted to double
}
//End of non-empty second value
}
//End of for the current line description
}
GlobalVariableSet(MLO_PREFIX+OrderTicket()+globalname,count);
VAR_STRING = VAR_STRING_ARRAY[0][0];
for(o=1;o<ArrayRange(VAR_STRING_ARRAY,0);o++)
{
if(VAR_STRING_ARRAY[o][1]!=""){
VAR_STRING = VAR_STRING+","+VAR_STRING_ARRAY[o][0]+"@"+VAR_STRING_ARRAY[o][1];
}
}
}