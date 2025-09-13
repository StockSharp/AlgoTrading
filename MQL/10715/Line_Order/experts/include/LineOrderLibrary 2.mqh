//+------------------------------------------------------------------+
//|                                             LineOrderLibrary.mq4 |
//|                                                       heelflip43 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "heelflip43"
#property link      "http://www.mql4.com/users/heelflip43"
#include <stderror.mqh>
#include <stdlib.mqh>
#include <WinUser32.mqh>


// If you feel as if a variable doesn't need to be external then delete the extern. It's just there for ease of use. 

extern  string LO_PREFIX="#"; // Prefix for the name of lines. ObjectName = LO_PREFIX+TicketNumber()+Specialty
extern  double LO_LOTS=0.1;
extern  double LO_PIPPROFIT=30;
extern  double LO_PIPSTOPLOSS=20;
extern  double LO_PIPTRAIL=0; // This trail acts like the default MT4 trail, once you are in profit by this much then the trail will start
extern  bool   LO_AUTO_INCLUDE_SL_TP = 1; // If no values entered then default values used
extern  bool   LO_CLOSE_ORDER_ON_DELETE = 1;  // Close order on deleting the main line else will re-create line next time
extern  bool   LO_PROMPT_DESCISIONS = 0; // Launches a message box when certain actions are taken such as removing a stop loss or take profit. For including in EA's should be 0
extern  bool   LO_CHECK_CLOSE_ORDER = 0; // When main order line is closed and LO_CLOSE_ORDER_ON_DELETE = 1 do you want to check that you want to close the order. For including in EA's should be 0
extern  int    LO_ALARM=1; // 0 = No alarm, 1 = Alert, 2 = Email(Not implemented yet), 3 = Send file(Not implemented)
extern  bool   LO_ECN=0; // Is the broker a ECN?
extern  int    MAGIC_NUMBER = -1;  // Set at -1 to apply to all currently open trades
extern  color  LO_ORDER_CLR=Gray; // Colour of open price line
extern  int    LO_ORDER_STYLE=STYLE_DASH; // Style of open price line
extern  color  LO_STOPLOSS_BUY_CLR=Red; // Colour of buy order's stop loss
extern  color  LO_STOPLOSS_SELL_CLR=OrangeRed; // Colour of sell order's stop loss
extern  int    LO_STOPLOSS_STYLE=STYLE_DASHDOT; // Style of order's stop loss
extern  color  LO_MOVE_STOPLOSS_CLR=Teal; // Colour of line which moves stoploss a specified stoploss when hit
extern  int    LO_MOVE_STOPLOSS_STYLE=STYLE_DASHDOT; // Style of line which moves stoploss a specified stoploss when hit
extern  color  LO_STOPLOSS_MOVE_CLR=Orange; // Colour of line to which to move stop loss to
extern  int    LO_STOPLOSS_MOVE_STYLE=STYLE_DASHDOT; // Style of line to which to move stop loss to
extern  color  LO_STOPLOSS_CLOSE_CLR=Red; // The colour of line which closes at a stop loss
extern  int    LO_STOPLOSS_CLOSE_STYLE=STYLE_DASHDOT; // The style of line which closes at a stop loss
extern  color  LO_TAKEPROFIT_BUY_CLR=Green; // Colour of the final take profit
extern  color  LO_TAKEPROFIT_SELL_CLR=Lime; // Colour of the final take profit
extern  int    LO_TAKEPROFIT_STYLE=STYLE_DASHDOT; // Style of line of final take profit
extern  color  LO_TAKEPROFIT_MOVE_CLR=Green; // Colour of the move take profit
extern  int    LO_TAKEPROFIT_MOVE_STYLE=STYLE_DASHDOT; // Style of the move take profit
extern  color  LO_TAKEPROFIT_CLOSE_CLR=Green; // Colour of the close take profit 
extern  int    LO_TAKEPROFIT_CLOSE_STYLE=STYLE_DASHDOT; // Style of the close take profit
extern  bool   UseLines = true;

double  Point.pip;
int     Point.round,Point.lot;

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
bool lock,timeUpdate=false;
int count;

#include <LineOrderStringLibrary.mqh>
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
Point.round = 1;
}else{
Point.pip = Point;
Point.round = 0;
}
if(MarketInfo(Symbol(), MODE_LOTSTEP)<0.1)Point.lot=2;else Point.lot=1;
}
void startInit(){
initVar();
for(int i=OrdersTotal()-1;i>=0;i--){
if(!OrderSelect(i,SELECT_BY_POS))continue;
int handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
if(ObjectFind(LO_PREFIX+OrderTicket())==-1){

}

}}
void  deinitVar(){
     switch(UninitializeReason())
       {
        case REASON_CHARTCLOSE: break;
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
static int count;
checkAlertLines();
if(count<5)count++;else{
checkLines();
UpdateLines(); 
cleanUpLines();
static datetime currTime;
if(currTime!=iTime(Symbol(),PERIOD_M15,0)){ 
cleanUpGlobal();
currTime=iTime(Symbol(),PERIOD_M15,0);
}
count=0;
}
}

void checkAlertLines()
{
if(!GlobalVariableCheck(MLO_PREFIX+"AlertLines"))GlobalVariableSet(MLO_PREFIX+"AlertLines",0);
for(int i=GlobalVariableGet(MLO_PREFIX+"AlertLines");i>=1;i--)
{
if(ObjectFind(MLO_PREFIX+"AlertLine"+i)==-1){
// Can't find line
for(int j=i;j<GlobalVariableGet(MLO_PREFIX+"AlertLines")-1;j++)
{
ObjectSetText(MLO_PREFIX+"AlertLine"+i,ObjectDescription(MLO_PREFIX+"AlertLine"+(i+1)));
ObjectSet(MLO_PREFIX+"AlertLine"+i,ObjectGet(MLO_PREFIX+"AlertLine"+(i+1),OBJPROP_PRICE1),OBJPROP_PRICE1);
}
ObjectDelete(MLO_PREFIX+"AlertLine"+(i+1));
GlobalVariableSet(MLO_PREFIX+"AlertLines",GlobalVariableGet(MLO_PREFIX+"AlertLines")-1);
}else
{
// Can find line

}}

}

int UpdateMultipleLines(int ticket)
{
string LOTS_STRING_ARRAY[][2],STOP_LOSS_PIP_STRING_ARRAY[][2],TAKE_PROFIT_PIP_STRING_ARRAY[][2],TS_STRING_ARRAY[][2];
OrderSelect(ticket, SELECT_BY_TICKET);
if(MAGIC_NUMBER>=0){
if(OrderMagicNumber()!=MAGIC_NUMBER)return (0);
}
string extra = "";
//if(OrderType()!=OP_BUY&&OrderType()!=OP_SELL)extra=" pend";
string str = ObjectDescription(LO_PREFIX+OrderTicket()+extra)+" ";
string STOP_LOSS_PIP_STRING = "";
return_value(str,"sl=",STOP_LOSS_PIP_STRING);
string TAKE_PROFIT_PIP_STRING = "";
return_value(str,"tp=",TAKE_PROFIT_PIP_STRING);
string STOP_LOSS_QUOTE_STRING = "";
return_value(str,"sq=",STOP_LOSS_QUOTE_STRING);
string TAKE_PROFIT_QUOTE_STRING = "";
return_value(str,"tq=",TAKE_PROFIT_QUOTE_STRING);
string TRAIL_STOP_PIP_STRING = "";
return_value(str,"ts=",TRAIL_STOP_PIP_STRING);
string LOT_ORDER_STRING = "";
return_value(str,"lo=",LOT_ORDER_STRING);
string alarm_string = "";
return_value(str,"alarm=",alarm_string);
int SL_order_count=0;
int TP_order_count=0;
int LO_order_count=0;
int TS_order_count=0;
Explode(STOP_LOSS_PIP_STRING,";",STOP_LOSS_PIP_STRING_ARRAY,1);
Explode(TAKE_PROFIT_PIP_STRING,";",TAKE_PROFIT_PIP_STRING_ARRAY,1);
Explode(LOT_ORDER_STRING,";",LOTS_STRING_ARRAY,1);
Explode(TRAIL_STOP_PIP_STRING,";",TS_STRING_ARRAY,1);

int error = GetLastError();if(error!=0)Print("Before : "+error+" "+ErrorDescription(error));
processMulitLine(OrderTicket(),str, STOP_LOSS_PIP_STRING_ARRAY,STOP_LOSS_PIP_STRING,StringConcatenate(LO_PREFIX,ticket," SL")," SL-Count"," sl=",LO_STOPLOSS_MOVE_CLR,LO_STOPLOSS_MOVE_STYLE,timeUpdate);
error = GetLastError();if(error!=0)Print("Stop Loss check: "+error+" "+ErrorDescription(error));
processMulitLine(OrderTicket(),str, TAKE_PROFIT_PIP_STRING_ARRAY,TAKE_PROFIT_PIP_STRING,StringConcatenate(LO_PREFIX,ticket," TP")," TP-Count"," tp=",LO_TAKEPROFIT_MOVE_CLR,LO_TAKEPROFIT_MOVE_STYLE,timeUpdate);
error = GetLastError();if(error!=0)Print("Take profit check: "+error+" "+ErrorDescription(error)+" "+ObjectFind(StringConcatenate(LO_PREFIX,ticket," TP")));
processMulitLine(OrderTicket(),str, LOTS_STRING_ARRAY,LOT_ORDER_STRING,StringConcatenate(LO_PREFIX,ticket," LO")," LO-Count"," lo=",Yellow,STYLE_DASHDOT,timeUpdate);
error = GetLastError();if(error!=0)Print("Lots check: "+error+" "+ErrorDescription(error));
processMulitLine(OrderTicket(),str, TS_STRING_ARRAY,TRAIL_STOP_PIP_STRING,StringConcatenate(LO_PREFIX,ticket," TS")," TS-Count"," ts=",Yellow,STYLE_DASHDOT,timeUpdate);
error = GetLastError();if(error!=0)Print("Trail check: "+error+" "+ErrorDescription(error));
string text = STOP_LOSS_PIP_STRING+STOP_LOSS_QUOTE_STRING+TAKE_PROFIT_PIP_STRING+TAKE_PROFIT_QUOTE_STRING+TRAIL_STOP_PIP_STRING+LOT_ORDER_STRING+alarm_string;
if(text!=ObjectDescription(LO_PREFIX+OrderTicket()+extra)){
ObjectSetText(LO_PREFIX+OrderTicket()+extra,text);
error = GetLastError();if(error!=0)Print("Set line text check: "+error+" "+ErrorDescription(error));
}
return (0);
}

int UpdateLines()
{
int error = GetLastError();
for(int j=OrdersTotal()-1;j>=0;j--)
{
bool update=false,pending=false,market=true,trail=false;
double LOTS,TS=LO_PIPTRAIL;
string LOTS_STRING="",STOP_LOSS_PIP_STRING="",TAKE_PROFIT_PIP_STRING="",TS_STRING="",STOP_LOSS_PIP="",TAKE_PROFIT_PIP="",linename,OrderNumber="";
string LOTS_STRING_ARRAY[][2],STOP_LOSS_PIP_STRING_ARRAY[][2],TAKE_PROFIT_PIP_STRING_ARRAY[][2],TS_STRING_ARRAY[][2];
if(!OrderSelect(j, SELECT_BY_POS,MODE_TRADES)){Print("OrderSelect failed");continue;}
if(OrderSymbol()!=Symbol())continue;
if(MAGIC_NUMBER>=0){
if(OrderMagicNumber()!=MAGIC_NUMBER)continue;
}
if(GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" Update"))if(GlobalVariableGet(MLO_PREFIX+OrderTicket()+" Update")!=iTime(Symbol(),1,0)){
timeUpdate=true;
GlobalVariableSet(MLO_PREFIX+OrderTicket()+" Update",iTime(Symbol(),1,0));
}else timeUpdate =false;
double STOP_LOSS_ACTUAL=OrderStopLoss(),TAKE_PROFIT_ACTUAL=OrderTakeProfit();
linename = LO_PREFIX+OrderTicket();
if(ObjectFind(linename)>-1){
// Line exists
bool updated = false;
if(ObjectFind(linename+" SL")==-1){
ObjectCreate(linename+" SL",OBJ_HLINE,0,0,OrderStopLoss());
if(OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)ObjectSet(linename+" SL",OBJPROP_COLOR,LO_STOPLOSS_BUY_CLR);ObjectSet(linename+" SL",OBJPROP_COLOR,LO_STOPLOSS_SELL_CLR);
ObjectSet(linename+" SL",OBJPROP_STYLE,LO_STOPLOSS_STYLE);
}
if(ObjectFind(linename+" TP")==-1){
ObjectCreate(linename+" TP",OBJ_HLINE,0,0,OrderTakeProfit());
if(OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)ObjectSet(linename+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_BUY_CLR);ObjectSet(linename+" TP",OBJPROP_COLOR,LO_TAKEPROFIT_SELL_CLR);
ObjectSet(linename+" TP",OBJPROP_STYLE,LO_TAKEPROFIT_STYLE);
}
string str = ObjectDescription(linename)+" ";
if(OrderType()==OP_BUY||OrderType()==OP_SELL){
if(ObjectGet(linename,OBJPROP_PRICE1)!=OrderOpenPrice())ObjectSet(linename,OBJPROP_PRICE1,OrderOpenPrice());
}else{
market=false;
if(ObjectGet(linename,OBJPROP_PRICE1)!=OrderOpenPrice()){
double pend = ObjectGet(linename,OBJPROP_PRICE1);
OrderModify(OrderTicket(),pend,OrderStopLoss(),OrderTakeProfit(),OrderExpiration());
}}
return_value(str,"order=",OrderNumber);
if(OrderNumber!=""){}else{
return_value(str,"sl=",STOP_LOSS_PIP_STRING);
if(STOP_LOSS_PIP_STRING=="")if(STOP_LOSS_ACTUAL>0){
STOP_LOSS_PIP_STRING=DoubleToStr((OrderOpenPrice()-OrderStopLoss())/Point.pip,Point.round);
if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)STOP_LOSS_PIP_STRING=DoubleToStr((OrderStopLoss()-OrderOpenPrice())/Point.pip,Point.round);
}else STOP_LOSS_PIP_STRING="N";
Explode(STOP_LOSS_PIP_STRING,";",STOP_LOSS_PIP_STRING_ARRAY,1);
for(int i=0;i<ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0);i++){
Explode(STOP_LOSS_PIP_STRING_ARRAY[i][0],"@", STOP_LOSS_PIP_STRING_ARRAY,2,i);
}

return_value(str,"tp=",TAKE_PROFIT_PIP_STRING);
if(TAKE_PROFIT_PIP_STRING=="")if(TAKE_PROFIT_ACTUAL>0){
TAKE_PROFIT_PIP_STRING=DoubleToStr((OrderTakeProfit()-OrderOpenPrice())/Point.pip,Point.round);
if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)TAKE_PROFIT_PIP_STRING=DoubleToStr((OrderOpenPrice()-OrderTakeProfit())/Point.pip,Point.round);
}else TAKE_PROFIT_PIP_STRING="N";
Explode(TAKE_PROFIT_PIP_STRING,";",TAKE_PROFIT_PIP_STRING_ARRAY,1);
for(i=0;i<ArrayRange(TAKE_PROFIT_PIP_STRING_ARRAY,0);i++){
Explode(TAKE_PROFIT_PIP_STRING_ARRAY[i][0],"@", TAKE_PROFIT_PIP_STRING_ARRAY,2,i);
}
return_value(str,"lo=",LOTS_STRING);
if(LOTS_STRING=="")LOTS_STRING = DoubleToStr(OrderLots(),Point.round);
Explode(LOTS_STRING,";",LOTS_STRING_ARRAY,1);
for(i=0;i<ArrayRange(LOTS_STRING_ARRAY,0);i++){
Explode(LOTS_STRING_ARRAY[i][0],"@", LOTS_STRING_ARRAY,2,i);
}

return_value(str,"ts=",TS_STRING);
if(TS_STRING=="")TS_STRING = DoubleToStr(LO_PIPTRAIL,Point.round);
Explode(TS_STRING,";",TS_STRING_ARRAY,1);
for(i=0;i<ArrayRange(TS_STRING_ARRAY,0);i++){
Explode(TS_STRING_ARRAY[i][0],"@", TS_STRING_ARRAY,2,i);
}

int handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
if(handle==-1){
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);
FileClose(handle);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
}
if(handle>-1)
    {
    if(FileSize(handle)>0)string str1=FileReadString(handle,FileSize(handle));else str1="";
    if(str1!=ObjectDescription(linename))
     {
     updated = true;GlobalVariableSet(MLO_PREFIX+OrderTicket()+" Update",0);
     str = ObjectDescription(linename);
     FileClose(handle);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);          
     FileWriteString(handle, str,StringLen(str));
     
int SL_order_count=0;
int TP_order_count=0;
int LO_order_count=0;
int TS_order_count=0;

for(i=0;i<ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0);i++){
Explode(STOP_LOSS_PIP_STRING_ARRAY[i][0],"@", STOP_LOSS_PIP_STRING_ARRAY,2,i);
if(STOP_LOSS_PIP_STRING_ARRAY[i][1]!=""){
SL_order_count++;

}else {STOP_LOSS_PIP = STOP_LOSS_PIP_STRING_ARRAY[i][0];if(StrToDouble(STOP_LOSS_PIP_STRING_ARRAY[i][0])==0&&StringSubstr(STOP_LOSS_PIP_STRING_ARRAY[i][0],0,1)=="0"){STOP_LOSS_PIP_STRING_ARRAY[i][0] = -0.1;}}
}
if(SL_order_count==ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0)&&SL_order_count!=0)STOP_LOSS_PIP = LO_PIPSTOPLOSS;else if(SL_order_count==ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0))STOP_LOSS_PIP = "N";

if((OrderType()==OP_BUY||OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)&&STOP_LOSS_PIP!="N"&&STOP_LOSS_PIP!="n"&&StrToDouble(STOP_LOSS_PIP)==0&&STOP_LOSS_PIP!="0")STOP_LOSS=OrderOpenPrice()-(pipString(STOP_LOSS_PIP,OrderOpenPrice(),"b")*Point.pip);else if((OrderType()==OP_BUY||OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)&&STOP_LOSS_PIP!="N"&&STOP_LOSS_PIP!="n")STOP_LOSS=OrderOpenPrice()-(StrToDouble(STOP_LOSS_PIP)*Point.pip);else if((OrderType()==OP_BUY||OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT))STOP_LOSS = 0;
if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&STOP_LOSS_PIP!="N"&&STOP_LOSS_PIP!="n"&&StrToDouble(STOP_LOSS_PIP)==0&&STOP_LOSS_PIP!="0")STOP_LOSS=OrderOpenPrice()+(pipString(STOP_LOSS_PIP,OrderOpenPrice(),"a")*Point.pip);else if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&STOP_LOSS_PIP!="N"&&STOP_LOSS_PIP!="n")STOP_LOSS=(StrToDouble(STOP_LOSS_PIP)*Point.pip)+OrderOpenPrice();else if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP))STOP_LOSS = 0;
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);
for(i=0;i<ArrayRange(TAKE_PROFIT_PIP_STRING_ARRAY,0);i++){
Explode(TAKE_PROFIT_PIP_STRING_ARRAY[i][0],"@", TAKE_PROFIT_PIP_STRING_ARRAY,2,i);

if(TAKE_PROFIT_PIP_STRING_ARRAY[i][1]!=""){
TP_order_count++;
}else TAKE_PROFIT_PIP = TAKE_PROFIT_PIP_STRING_ARRAY[i][0];
}
if(TP_order_count==ArrayRange(TAKE_PROFIT_PIP_STRING_ARRAY,0)&&TP_order_count!=0)TAKE_PROFIT_PIP = LO_PIPPROFIT; 

if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&TAKE_PROFIT_PIP!="N"&&TAKE_PROFIT_PIP!="n"&&StrToDouble(TAKE_PROFIT_PIP)==0&&TAKE_PROFIT_PIP!="0")TAKE_PROFIT=OrderOpenPrice()+(pipString(TAKE_PROFIT_PIP,OrderOpenPrice(),"a")*Point.pip);else if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&TAKE_PROFIT_PIP!="N"&&TAKE_PROFIT_PIP!="n")TAKE_PROFIT=OrderOpenPrice()+(StrToDouble(TAKE_PROFIT_PIP)*Point.pip);else if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP))TAKE_PROFIT = 0;
if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&TAKE_PROFIT_PIP!="N"&&TAKE_PROFIT_PIP!="n"&&StrToDouble(TAKE_PROFIT_PIP)==0&&TAKE_PROFIT_PIP!="0")TAKE_PROFIT=OrderOpenPrice()-(pipString(TAKE_PROFIT_PIP,OrderOpenPrice(),"b")*Point.pip);else if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&TAKE_PROFIT_PIP!="N"&&TAKE_PROFIT_PIP!="n")TAKE_PROFIT=OrderOpenPrice()-(StrToDouble(TAKE_PROFIT_PIP)*Point.pip);else if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP))TAKE_PROFIT = 0;
ObjectSet(LO_PREFIX+OrderTicket()+" TP",OBJPROP_PRICE1,TAKE_PROFIT);
string LOTS_STRING1,LOTS_STRING2,LOTS_STRING_ARRAY1[][2],LOTS_STRING_ARRAY2[][2];
return_value(str1,"lo=",LOTS_STRING1);
return_value(str,"lo=", LOTS_STRING2);

if(LOTS_STRING1!=LOTS_STRING2&&LOTS_STRING2!=""&&LOTS_STRING1!=""){
error=GetLastError();
Explode(LOTS_STRING1,";",LOTS_STRING_ARRAY1,1);
Explode(LOTS_STRING2,";",LOTS_STRING_ARRAY2,1);
error=GetLastError();
if(error!=0)Print(ErrorDescription(error));
if(StringSubstr(LOTS_STRING2,0,1)=="+"){LOTS_STRING_ARRAY2[0][0]=DoubleToStr(OrderLots()+StrToDouble(StringSubstr(LOTS_STRING2,1)),Point.lot);}
if(StringSubstr(LOTS_STRING2,0,1)=="-"){LOTS_STRING_ARRAY2[0][0]=DoubleToStr(OrderLots()-StrToDouble(StringSubstr(LOTS_STRING2,1)),Point.lot);Print("Should lower lots now. "+LOTS_STRING_ARRAY2[0][0]);}
Print("LOTS_STRING1: "+LOTS_STRING1+" LOTS_STRING2: "+LOTS_STRING2);
Print("Here lies the magic of changing lot sizes.#.\""+LOTS_STRING1+"\".#.\""+LOTS_STRING2+"\".#@++ \""+LOTS_STRING_ARRAY2[0][0]+"\" size1:"+ArrayRange(LOTS_STRING_ARRAY1,0)+" size 2: "+ArrayRange(LOTS_STRING_ARRAY2,0));
double value = StrToDouble(LOTS_STRING_ARRAY2[0][0]);
if(value!=0&&(OrderLots()-value)>MarketInfo(Symbol(),MODE_MINLOT)){
if(value>OrderLots()){
//Open more lots
}else{
//Close lots
Print("Need to close lots");
int ticket=OrderTicket();
if(!OrderClose(OrderTicket(),(OrderLots()-value), OrderClosePrice(), MarketInfo(Symbol(), MODE_SPREAD)))Print(ErrorDescription(GetLastError()));
update=true;
OrderSelect(OrdersTotal()-1,SELECT_BY_POS);
int handle1 = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_WRITE);
FileWriteString(handle1, str,StringLen(str));
FileClose(handle1);
}
}else if(value==0&&LOTS_STRING_ARRAY2[0][0]!="0"){

}else if(value<=0){
if(!OrderClose(OrderTicket(),OrderLots(), OrderClosePrice(), MarketInfo(Symbol(), MODE_SPREAD)))Print(ErrorDescription(GetLastError()));
}
}
for(i=0;i<ArrayRange(TS_STRING_ARRAY,0);i++){
Explode(TS_STRING_ARRAY[i][0],"@", TS_STRING_ARRAY,2,i);
if(TS_STRING_ARRAY[i][1]!=""){
TS_order_count++;
}else TS = mainString(TS_STRING_ARRAY[i][0],true);

}
if(TS_order_count==ArrayRange(TS_STRING_ARRAY,0)&&TS_order_count!=0)TS = LO_PIPTRAIL;     
     
     }else{
     // File hasn't changed
      if(TS_STRING_ARRAY[0][0]!="0"&&StrToDouble(TS_STRING_ARRAY[0][0])==0)TS = mainString(TS_STRING_ARRAY[0][0],true);else TS = StrToDouble(TS_STRING_ARRAY[0][0]);
     }
     FileClose(handle);
    }    

if(TS>0&&market){
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
STOP_LOSS_PIP = DoubleToStr((OrderOpenPrice()-STOP_LOSS)/Point.pip,1);
Print("TrailStop loss: "+STOP_LOSS);
trail=true;
}
if(OrderType()==OP_SELL&&(OrderClosePrice()+(TS*Point.pip))<OrderStopLoss()){
STOP_LOSS = OrderClosePrice()+(TS*Point.pip);
ObjectSet(LO_PREFIX+OrderTicket()+" SL",OBJPROP_PRICE1,STOP_LOSS);
trail=true;
}}}}
//End of OrderNumber==""
}
STOP_LOSS_ACTUAL = NormalizeDouble(ObjectGet(linename+" SL",OBJPROP_PRICE1),Digits);  

if(NormalizeDouble(ObjectGet(linename+" SL",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderStopLoss(),Digits)){
ObjectSet(linename+" SL",OBJPROP_PRICE1,NormalizeDouble(ObjectGet(linename+" SL",OBJPROP_PRICE1),Digits));

if(((StrToDouble(STOP_LOSS_PIP_STRING_ARRAY[0][0])!=0&&STOP_LOSS_PIP_STRING_ARRAY[0][0]!=DoubleToStr(0,Point.round)))){
STOP_LOSS_PIP_STRING_ARRAY[0][0] = DoubleToStr((OrderOpenPrice()-STOP_LOSS_ACTUAL)/Point.pip,Point.round);
if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)STOP_LOSS_PIP_STRING_ARRAY[0][0] = DoubleToStr((STOP_LOSS_ACTUAL-OrderOpenPrice())/Point.pip,Point.round);
if(STOP_LOSS_ACTUAL==0)STOP_LOSS_PIP_STRING_ARRAY[0][0] = "N";
}
update=true;
}

if(NormalizeDouble(ObjectGet(linename+" TP",OBJPROP_PRICE1),Digits)!=NormalizeDouble(OrderTakeProfit(),Digits)){
TAKE_PROFIT_ACTUAL = NormalizeDouble(ObjectGet(linename+" TP",OBJPROP_PRICE1),Digits);
if(MathAbs(TAKE_PROFIT_ACTUAL-OrderOpenPrice())<MarketInfo(Symbol(), MODE_STOPLEVEL)*Point)if((OrderType()==OP_BUY||OrderType()==OP_BUYSTOP||OrderType()==OP_BUYLIMIT)&&OrderTakeProfit()>OrderOpenPrice()+MarketInfo(Symbol(), MODE_STOPLEVEL)*Point)ObjectSet(linename+" TP",OBJPROP_PRICE1,OrderOpenPrice()+MarketInfo(Symbol(), MODE_STOPLEVEL)*Point);else if((OrderType()==OP_SELL||OrderType()==OP_SELLSTOP||OrderType()==OP_SELLLIMIT)&&OrderTakeProfit()<OrderOpenPrice()-MarketInfo(Symbol(), MODE_STOPLEVEL)*Point)ObjectSet(linename+" TP",OBJPROP_PRICE1,OrderOpenPrice()-MarketInfo(Symbol(), MODE_STOPLEVEL)*Point);
ObjectSet(linename+" TP",OBJPROP_PRICE1,NormalizeDouble(ObjectGet(linename+" TP",OBJPROP_PRICE1),Digits));
if(StrToDouble(TAKE_PROFIT_PIP_STRING_ARRAY[0][0])!=0){
TAKE_PROFIT_PIP_STRING_ARRAY[0][0] = DoubleToStr((TAKE_PROFIT_ACTUAL-OrderOpenPrice())/Point.pip,Point.round);
if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)TAKE_PROFIT_PIP_STRING_ARRAY[0][0] = DoubleToStr((OrderOpenPrice()-TAKE_PROFIT_ACTUAL)/Point.pip,Point.round);
if(TAKE_PROFIT_ACTUAL==0)TAKE_PROFIT_PIP_STRING_ARRAY[0][0] = "N";
}
update=true;
}   
if(update){
Print("Updating... Stop Loss: "+STOP_LOSS_ACTUAL+" Take Profit: "+TAKE_PROFIT_ACTUAL);
if(!OrderModify(OrderTicket(),OrderOpenPrice(),STOP_LOSS_ACTUAL,TAKE_PROFIT_ACTUAL,OrderExpiration())){
Print("OrderModify has failed");
ObjectSet(linename+" SL",OBJPROP_PRICE1,OrderStopLoss());
ObjectSet(linename+" TP",OBJPROP_PRICE1,OrderTakeProfit());
}
STOP_LOSS_PIP_STRING = STOP_LOSS_PIP_STRING_ARRAY[0][0];
if(STOP_LOSS_PIP_STRING_ARRAY[0][1]!="")STOP_LOSS_PIP_STRING = STOP_LOSS_PIP_STRING + "@"+STOP_LOSS_PIP_STRING_ARRAY[0][1];
for(i=1;i<ArrayRange(STOP_LOSS_PIP_STRING_ARRAY,0);i++){
if(STOP_LOSS_PIP_STRING_ARRAY[i][1]!=""){STOP_LOSS_PIP_STRING=STOP_LOSS_PIP_STRING+";"+STOP_LOSS_PIP_STRING_ARRAY[i][0]+"@"+STOP_LOSS_PIP_STRING_ARRAY[i][1];}
}  
TAKE_PROFIT_PIP_STRING = TAKE_PROFIT_PIP_STRING_ARRAY[0][0];
if(TAKE_PROFIT_PIP_STRING_ARRAY[0][1]!="")TAKE_PROFIT_PIP_STRING = TAKE_PROFIT_PIP_STRING + "@"+TAKE_PROFIT_PIP_STRING_ARRAY[0][1];
for(i=1;i<ArraySize(TAKE_PROFIT_PIP_STRING_ARRAY);i++){
if(TAKE_PROFIT_PIP_STRING_ARRAY[i][1]!="")TAKE_PROFIT_PIP_STRING=TAKE_PROFIT_PIP_STRING+TAKE_PROFIT_PIP_STRING_ARRAY[i][0]+"@"+TAKE_PROFIT_PIP_STRING_ARRAY[i][1];
}
TS_STRING = TS_STRING_ARRAY[0][0];
if(TS_STRING_ARRAY[0][1]!="")TS_STRING = TS_STRING + "@"+TS_STRING_ARRAY[0][1];
for(i=1;i<ArrayRange(TS_STRING_ARRAY,0);i++){
if(TS_STRING_ARRAY[i][1]!="")TS_STRING=TS_STRING+TS_STRING_ARRAY[i][0]+"@"+TS_STRING_ARRAY[i][1];
}
LOTS_STRING = DoubleToStr(OrderLots(), Point.lot);
if(LOTS_STRING_ARRAY[0][1]!="")LOTS_STRING = LOTS_STRING + ";"+LOTS_STRING_ARRAY[0][0]+"@"+LOTS_STRING_ARRAY[0][1];
for(i=1;i<ArrayRange(LOTS_STRING_ARRAY,0);i++){
if(LOTS_STRING_ARRAY[i][1]!="")LOTS_STRING=LOTS_STRING+LOTS_STRING_ARRAY[i][0]+"@"+LOTS_STRING_ARRAY[i][1];
}
string text = "sl="+ STOP_LOSS_PIP_STRING+" tp="+TAKE_PROFIT_PIP_STRING+" ts="+TS_STRING+" lo="+LOTS_STRING;
ObjectSetText(linename, text);
}

}else{
// Line needs to created
if(GlobalVariableCheck(MLO_PREFIX+OrderTicket())&&LO_CLOSE_ORDER_ON_DELETE){
OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),MarketInfo(Symbol(),MODE_SPREAD));
cleanUpLines();
Print("Order should be closing now");
continue;
}
ObjectCreate(linename, OBJ_HLINE, 0, 0, OrderOpenPrice());
ObjectSet(linename,OBJPROP_COLOR, Gray);
ObjectSet(linename,OBJPROP_STYLE, STYLE_DASH);
handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_BIN|FILE_READ);
if(handle>-1)
    {
    if(FileSize(handle)>0) str1=FileReadString(handle,FileSize(handle));else str1="";
    ObjectSetText(linename,str1);
    FileClose(handle);
    }else{
    if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&OrderStopLoss()!=0)STOP_LOSS_PIP_STRING = DoubleToStr((OrderOpenPrice()-OrderStopLoss())/Point.pip,Point.round);
    if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&OrderStopLoss()!=0)STOP_LOSS_PIP_STRING = DoubleToStr((OrderStopLoss()-OrderOpenPrice())/Point.pip,Point.round);
    if(OrderStopLoss()==0)STOP_LOSS_PIP_STRING = "N";
    if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&OrderStopLoss()!=0)TAKE_PROFIT_PIP_STRING = DoubleToStr((OrderTakeProfit()-OrderOpenPrice())/Point.pip,Point.round);
    if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&OrderStopLoss()!=0)TAKE_PROFIT_PIP_STRING = DoubleToStr((OrderOpenPrice()-OrderTakeProfit())/Point.pip,Point.round);
    if(OrderStopLoss()==0)TAKE_PROFIT_PIP_STRING = "N";
    TS_STRING = DoubleToStr(LO_PIPTRAIL,Point.round);
    LOTS_STRING = DoubleToStr(OrderLots(),1);
    }
    GlobalVariableSet(MLO_PREFIX+OrderTicket(),1);
}
GetLastError();
//UpdateMultipleLines(OrderTicket());
error = GetLastError();if(error!=0)Print("End of loop error check: "+error+" "+ErrorDescription(error));
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
 cleanUpGlobal();
 }
 ObjectDelete(name);
}

}}}}
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
if(dimension == 1){
 //ArrayResize(arr,0);
 ArrayResize(arr,Count(str,delimiter));
 }
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
int error = GetLastError();
error = GetLastError(); 
 int inx_start=StringFind(body,search,0);    
 if(error!=0)Print(error+"."+body+"."+search+".");
 if(inx_start>=0){
         inx_start=StringLen(search)+inx_start;
     int inx_stop=StringFind(body," ",inx_start); 
 if(position=="n"){
 variable = StringSubstr(body,inx_start,inx_stop-inx_start);
 }else if(position=="b"){
 variable = StringConcatenate(addtion,StringSubstr(body,inx_start,inx_stop-inx_start));
 }else if(position=="a"){
 variable = StringConcatenate(StringSubstr(body,inx_start,inx_stop-inx_start),addtion);
 }}else variable="";
}

void processMulitLine(int ticket, string str, string& VAR_STRING_ARRAY[][],string& VAR_STRING,string lineName,string globalname, string description,color LineColour,int LineStyle,bool timeUpdate){
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
if(timeUpdate)value = mainString(VAR_STRING_ARRAY[o][1]);else value = ObjectGet(lineName+count,OBJPROP_PRICE1);
if(ObjectFind(lineName+count)>-1){
// Line does exist
double SL_Level = value;
 if(timeUpdate)ObjectSet(lineName+count,OBJPROP_PRICE1,value);
 str = ObjectDescription(lineName+count);
string store;
return_value(str,"dir=",store);
if(NormalizeDouble(ObjectGet(lineName+count,OBJPROP_PRICE1),Digits)!=value&&!((store == "a"&&Bid>SL_Level)||(store== "b"&& Ask<SL_Level))){
//if(StrToDouble(VAR_STRING_ARRAY[o][1])<Bid)ObjectSetText(lineName+count,"dir=b value="+VAR_STRING_ARRAY[o][0]);else ObjectSetText(lineName+count,"dir=a value="+VAR_STRING_ARRAY[o][0]);
if(value<Bid)ObjectSetText(lineName+count,"dir=b value="+VAR_STRING_ARRAY[o][0]);else ObjectSetText(lineName+count,"dir=a value="+VAR_STRING_ARRAY[o][0]);
}
if((store == "a"&&Bid>SL_Level)||(store== "b"&& Ask<SL_Level)){
if(OrderType()==OP_BUY||OrderType()==OP_SELL){
if(StringFind(VAR_STRING_ARRAY[o][0],"c+",0)==-1&&StringFind(VAR_STRING_ARRAY[o][0],"c-",0)==-1){
Print("Level hit + "+store+" "+SL_Level);
VAR_STRING_ARRAY[0][0]=VAR_STRING_ARRAY[o][0];
}else{
return_value(VAR_STRING_ARRAY[0][0],description,store);
if(StringFind(VAR_STRING_ARRAY[o][0],"c+",0)>-1){
VAR_STRING_ARRAY[0][0]=description+DoubleToStr(StrToDouble(store)+StrToDouble(StringSubstr(VAR_STRING_ARRAY[o][0],2)),Point.round);
}else{
VAR_STRING_ARRAY[0][0]=description+DoubleToStr(StrToDouble(store)-StrToDouble(StringSubstr(VAR_STRING_ARRAY[o][0],2)),Point.round);
}}
VAR_STRING_ARRAY[o][1]= "";
ObjectDelete(lineName+count);
for(int i=count;i<GlobalVariableGet(MLO_PREFIX+OrderTicket()+globalname);i++){
ObjectCreate(lineName+i,OBJ_HLINE,0,0,ObjectGet(lineName+(i+1),OBJPROP_PRICE1));
ObjectDelete(lineName+(i+1));
}}}
}else{
//Line doesn't exist

ObjectCreate(lineName+count,OBJ_HLINE,0,0,value);

ObjectSet(lineName+count,OBJPROP_COLOR,LineColour);
//if(StrToDouble(VAR_STRING_ARRAY[o][1])<Bid)ObjectSetText(lineName+count,"dir=b value="+VAR_STRING_ARRAY[o][0]);else ObjectSetText(lineName+count,"dir=a value="+VAR_STRING_ARRAY[o][0]);
if(value<Bid)ObjectSetText(lineName+count,"dir=b value="+VAR_STRING_ARRAY[o][0]);else ObjectSetText(lineName+count,"dir=a value="+VAR_STRING_ARRAY[o][0]);

}
}else {
if(ObjectFind(lineName+count)>-1){
//Line does exist
 SL_Level = ObjectGet(lineName+count,OBJPROP_PRICE1);

str = ObjectDescription(lineName+count);
if(NormalizeDouble(SL_Level,Digits)!=value){
VAR_STRING_ARRAY[o][1]=DoubleToStr(SL_Level,Digits);
if(StrToDouble(VAR_STRING_ARRAY[o][1])<Bid)ObjectSetText(lineName+count,"dir=b value="+VAR_STRING_ARRAY[o][0]);else ObjectSetText(lineName+count,"dir=a value="+VAR_STRING_ARRAY[o][0]);
}
 store="";
return_value(str,"dir=",store);
//if(Bid>SL_Level)Print("all dir=a should fill");
if((store == "a"&&Bid>SL_Level)||(store== "b"&& Ask<SL_Level)){
Print("Should change");
if(OrderType()==OP_BUY||OrderType()==OP_SELL){
if(StringFind(VAR_STRING_ARRAY[o][0],"c+",0)==-1&&StringFind(VAR_STRING_ARRAY[o][0],"c-",0)==-1){
if(StrToDouble(VAR_STRING_ARRAY[o][0])!=0)VAR_STRING_ARRAY[o][0]=DoubleToStr(StrToDouble(VAR_STRING_ARRAY[o][0]),Point.round);
VAR_STRING_ARRAY[0][0]=VAR_STRING_ARRAY[o][0];
}else{
return_value(VAR_STRING_ARRAY[0][0],description,store);
if(StringFind(VAR_STRING_ARRAY[o][0],"c+",0)>-1){
VAR_STRING_ARRAY[0][0]=DoubleToStr(StrToDouble(store)+StrToDouble(StringSubstr(VAR_STRING_ARRAY[o][0],2)),Point.round);
}else{
VAR_STRING_ARRAY[0][0]=DoubleToStr(StrToDouble(store)-StrToDouble(StringSubstr(VAR_STRING_ARRAY[o][0],2)),Point.round);
}

}
VAR_STRING_ARRAY[o][1]= "";
ObjectDelete(lineName+count);
for(i=count;i<GlobalVariableGet(MLO_PREFIX+OrderTicket()+globalname);i++){
ObjectCreate(lineName+i,OBJ_HLINE,0,0,ObjectGet(lineName+(i+1),OBJPROP_PRICE1));
ObjectDelete(lineName+(i+1));
}
}
}

}else{
//Line doesn't exist
ObjectCreate(lineName+count,OBJ_HLINE,0,0,StrToDouble(VAR_STRING_ARRAY[o][1]));

ObjectSet(lineName+count,OBJPROP_COLOR,LineColour);
if(StrToDouble(VAR_STRING_ARRAY[o][1])<Bid)ObjectSetText(lineName+count,"dir=b value="+VAR_STRING_ARRAY[o][0]);else ObjectSetText(lineName+count,"dir=a value="+VAR_STRING_ARRAY[o][0]);

}
//End of second value being successfully converted to double
}
//End of non-empty second value
}else{
//Start of empty second value
if(timeUpdate&&o==0&&(description==" sl="||description==" tp=")){
value=0;
if(description==" sl="){
if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n"&&StrToDouble(VAR_STRING_ARRAY[o][0])==0&&VAR_STRING_ARRAY[o][0]!=DoubleToStr(0,Point.round))value=mainString(VAR_STRING_ARRAY[o][0]);else if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n")value=(OrderOpenPrice()-StrToDouble(VAR_STRING_ARRAY[o][0])*Point.pip);else if(OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)value = 0;
if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n"&&StrToDouble(VAR_STRING_ARRAY[o][0])==0&&VAR_STRING_ARRAY[o][0]!=DoubleToStr(0,Point.round))value=mainString(VAR_STRING_ARRAY[o][0]);else if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n")value=(StrToDouble(VAR_STRING_ARRAY[o][0])*Point.pip)+OrderOpenPrice();else if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)value = 0;
}else if(description==" tp="){
if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n"&&StrToDouble(VAR_STRING_ARRAY[o][0])==0&&VAR_STRING_ARRAY[o][0]!=DoubleToStr(0,Point.round))value=mainString(VAR_STRING_ARRAY[o][0]);else if((OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n")value=(OrderOpenPrice()+StrToDouble(VAR_STRING_ARRAY[o][0])*Point.pip);else if(OrderType()==OP_BUY||OrderType()==OP_BUYLIMIT||OrderType()==OP_BUYSTOP)value = 0;
if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n"&&StrToDouble(VAR_STRING_ARRAY[o][0])==0&&VAR_STRING_ARRAY[o][0]!=DoubleToStr(0,Point.round))value=mainString(VAR_STRING_ARRAY[o][0]);else if((OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)&&VAR_STRING_ARRAY[o][0]!="N"&&VAR_STRING_ARRAY[o][0]!="n")value=(OrderOpenPrice()-StrToDouble(VAR_STRING_ARRAY[o][0])*Point.pip);else if(OrderType()==OP_SELL||OrderType()==OP_SELLLIMIT||OrderType()==OP_SELLSTOP)value = 0;
}
if(ObjectFind(lineName)!=-1)ObjectSet(lineName,OBJPROP_PRICE1,value);
//Print(description+" @"+VAR_STRING_ARRAY[o][0]+"@ "+DoubleToStr(value,Digits)+" iMA:"+NormalizeDouble(iMA(NULL,5,50,0,MODE_SMA,PRICE_CLOSE,0),Digits));
}
if(!GlobalVariableCheck(MLO_PREFIX+OrderTicket()+" Update"))GlobalVariableSet(MLO_PREFIX+OrderTicket()+" Update",0);

}

//End of for the current line description
}
GlobalVariableSet(MLO_PREFIX+OrderTicket()+globalname,count);
if(VAR_STRING_ARRAY[0][0]!="")VAR_STRING = description+VAR_STRING_ARRAY[0][0];
for(o=1;o<ArrayRange(VAR_STRING_ARRAY,0);o++)
{
if(VAR_STRING_ARRAY[o][1]!=""){
VAR_STRING = VAR_STRING+";"+VAR_STRING_ARRAY[o][0]+"@"+VAR_STRING_ARRAY[o][1];
}
}
}

void cleanUpGlobal()
{
for(int i=GlobalVariablesTotal()-1;i>=0;i--){
if(StringFind(GlobalVariableName(i),MLO_PREFIX)>-1){

double text = StrToDouble(StringSubstr(GlobalVariableName(i),StringLen(MLO_PREFIX),StringFind(GlobalVariableName(i)," ",StringLen(MLO_PREFIX))));
if(OrderSelect(text,SELECT_BY_TICKET)==true){
if(OrderCloseTime()>0){
Print("Deleting excess global variable: "+GlobalVariableName(i));
GlobalVariableDel(GlobalVariableName(i));
}}}}
for(i=OrdersHistoryTotal();i>(OrdersHistoryTotal()-10);i--){
if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))continue;
int handle = FileOpen(MLO_PREFIX+OrderTicket()+".txt",FILE_READ);
if(handle>-1){
FileClose(handle);
GetLastError();
Print("Deleting excess file: "+MLO_PREFIX+OrderTicket()+".txt");
if(handle>0){
for(int j=handle-1;j>0;j--)
{
FileClose(j);
}
}
FileDelete(MLO_PREFIX+OrderTicket()+".txt");
int lastError=GetLastError();
if(lastError!=0){
lastError=GetLastError();
FileDelete(MLO_PREFIX+OrderTicket()+".txt");
lastError=GetLastError();
if(lastError!=0){
Alert("Deleting file went wrong. "+ErrorDescription(lastError)+" "+MLO_PREFIX+OrderTicket()+".txt handle: "+handle);
}else Print("Closing all the files worked");
}else Print("File delete went ok.");

}}

}


int return_timeframe(string var)
{
if(var!="0"&&StrToInteger(var)==0)
{
if(var=="curr"||var=="CURRRENT"||var=="0")return (0);
if(var=="PERIOD_M1"||var=="M1"||var=="1")return (PERIOD_M1);
if(var=="PERIOD_M5"||var=="M5"||var=="5")return (PERIOD_M5);
if(var=="PERIOD_M15"||var=="M15"||var=="15")return (PERIOD_M15);
if(var=="PERIOD_M30"||var=="M30"||var=="30")return (PERIOD_M30);
if(var=="PERIOD_H1"||var=="H1"||var=="60")return (PERIOD_H1);
if(var=="PERIOD_H4"||var=="H4"||var=="240")return (PERIOD_H4);
if(var=="PERIOD_D1"||var=="D1")return (PERIOD_D1);
if(var=="PERIOD_W1"||var=="W1")return (PERIOD_W1);
if(var=="PERIOD_MN1"||var=="MN1")return (PERIOD_MN1);
if(var=="")return (NULL);
}else return(StrToInteger(var));

}

int return_price_constant(string var)
{
if(var!="0"&&StrToDouble(var)==0)
{
if(var=="PRICE_CLOSE"||var=="close"||var=="CLOSE"||var=="Close")return (PRICE_CLOSE);
if(var=="PRICE_OPEN"||var=="open"||var=="OPEN"||var=="Open")return (PRICE_OPEN);
if(var=="PRICE_HIGH"||var=="high"||var=="HIGH"||var=="High")return (PRICE_HIGH);
if(var=="PRICE_LOW"||var=="low"||var=="LOW"||var=="Low")return (PRICE_LOW);
if(var=="PRICE_MEDIAN"||var=="median"||var=="MEDIAN")return (PRICE_MEDIAN);
if(var=="PRICE_TYPICAL"||var=="typical"||var=="TYPICAL")return (PRICE_TYPICAL);
if(var=="PRICE_WEIGHTED"||var=="WEIGHTED"||var=="weighted")return (PRICE_WEIGHTED);
}else return(StrToInteger(var));
}
double pipString(string var,double open,string direction)
{
double value = mainString(var);
if(direction=="b")value = open - value;else value = value - open;
value = value/Point.pip;
return (value);
}

double mainString(string var,bool convert=false)
{
double result;
int count=1,start,find;
string store[],localvar=var;
double dblstore[];
for(int i=0;i<StringLen(var);i++)
{if(StringSubstr(localvar,i,1)=="+")count++;if(StringSubstr(localvar,i,1)=="-")count++;if(StringSubstr(localvar,i,1)=="*")count++;if(StringSubstr(localvar,i,1)=="/")count++;}
ArrayResize(store,count);ArrayResize(dblstore,count);
for(i=0;i<ArraySize(store);i++)
{find = StringLen(localvar);if(StringFind(localvar,"+",start)>-1&&StringFind(localvar,"+",start)<find)find=StringFind(localvar,"+",start);if(StringFind(localvar,"-",start)>-1&&StringFind(localvar,"-",start)<find)find=StringFind(localvar,"-",start);if(StringFind(localvar,"*",start)>-1&&StringFind(localvar,"*",start)<find)find=StringFind(localvar,"*",start);if(StringFind(localvar,"/",start)>-1&&StringFind(localvar,"/",start)<find)find=StringFind(localvar,"/",start);store[i] = StringSubstr(localvar,start,find-start);
if(i!=0){
string operator = StringSubstr(localvar,start-1,1);
if(operator=="*"||operator=="/"){
if(operator=="*")dblstore[i] = dblstore[i-1]*setUpProcessString(store[i]);else if(operator=="/")dblstore[i] = dblstore[i -1]/setUpProcessString(store[i]);
localvar=StringSetChar(localvar,start-1,'+');
dblstore[i-1] =0;
}else dblstore[i]=setUpProcessString(store[i]);
}else dblstore[i]=setUpProcessString(store[i]);
start=find+1;
}
start=0;

for(i=0;i<ArraySize(store);i++)
{
find = StringLen(localvar);
if(StringFind(localvar,"+",start)>-1&&StringFind(localvar,"+",start)<find)find=StringFind(localvar,"+",start);
if(StringFind(localvar,"-",start)>-1&&StringFind(localvar,"-",start)<find)find=StringFind(localvar,"-",start);
if(StringFind(localvar,"*",start)>-1&&StringFind(localvar,"*",start)<find)find=StringFind(localvar,"*",start);
if(StringFind(localvar,"/",start)>-1&&StringFind(localvar,"/",start)<find)find=StringFind(localvar,"/",start);
if(i!=0){
 operator = StringSubstr(localvar,start-1,1);
if(operator=="+")result=result+dblstore[i];else result=result-dblstore[i];
}else result = dblstore[i];
start=find+1;
}
if(convert)result=NormalizeDouble(result/Point.pip,Point.round);else result = NormalizeDouble(result,Digits);
return (result);
}

double setUpProcessString(string var)
{
string function;
string explodeVar[];
function = StringSubstr(var,0,StringFind(var,"("));
string variable = "";
int count;
for(int i=StringFind(var,"(");i<StringLen(var);i++){
if(StringSubstr(var,i,1)=="(")count++;
if(StringSubstr(var,i,1)==")")count--;
if(count==0){
variable = StringSubstr(var,StringFind(var,"(")+1,i-StringFind(var,"(")-1);
break;
}
}
ArrayResize(explodeVar,Count(variable,","));
int start,find;
for(i=0;i<ArraySize(explodeVar);i++)
{
find=StringFind(variable,",",start);
if(find==-1)find=StringLen(variable);
explodeVar[i] = StringSubstr(variable,start,find-start);
start = StringFind(variable,",",start)+1;
}
if(var!="0"&&StrToDouble(var)==0)
{
return (processString(function,explodeVar));
}else return (StrToDouble(var));
}

string LotCalc(string var,string stopLoss)
{
string lots = DoubleToStr(LO_LOTS,1),stoploss=DoubleToStr(LO_PIPSTOPLOSS,1);
double exchange = MarketInfo(Symbol(),MODE_TICKVALUE);
if(stopLoss!="")stoploss=stopLoss;
if(StringFind(var,"$")>-1){
double money = StrToDouble(StringSubstr(var,1));
lots = DoubleToStr((money)/(StrToDouble(stoploss)*exchange*MathPow(10,Point.round)),1);
}else if(StringFind(var,"%")>-1){

 money = AccountFreeMargin()*(StrToDouble(StringSubstr(var,0,StringLen(var)-1))/100);
lots = DoubleToStr((money)/(StrToDouble(stoploss)*exchange*MathPow(10,Point.round)),1);
}else{
lots=StrToDouble(var);
}

return (lots);
}
string StringReplace(string text,string search,string replace){
string before,after;
if(StringFind(text,search)>-1){
before = StringSubstr(text,0,StringFind(text,search));
after = StringSubstr(text,StringFind(text,search)+StringLen(search));
return (before+replace+after);
}
return (text);
}

   void checkLines()
{
   string name="",store="",linename;
   int type;
   for(int i=ObjectsTotal()-1;i>=0;i--)
   {
   bool newtrade = false;
   name = ObjectName(i);
   
   if(StringFind(name,LO_PREFIX+"buy")>-1||StringFind(name,LO_PREFIX+"sell")>-1||StringFind(name,LO_PREFIX+"buypend")>-1||StringFind(name,LO_PREFIX+"sellpend")>-1||StringFind(name,LO_PREFIX+"buysl")>-1||StringFind(name,LO_PREFIX+"buytp")>-1||StringFind(name,LO_PREFIX+"sellsl")>-1||StringFind(name,LO_PREFIX+"selltp")>-1){
   newtrade = true;
   GetLastError();
   if(OrderProcess(name))ObjectDelete(name);else Print("Processing of the "+name+" line has failed, will try again soon "+ErrorDescription(GetLastError()));
   }
   
   }
initVar();
}
bool OrderProcess(string name){
string sl,tp,lo,ts,sq,tq,alarm,str=ObjectDescription(name);
return_value(str,"sl=",sl);
if(sl==""&&LO_AUTO_INCLUDE_SL_TP==1)sl=DoubleToStr(LO_PIPSTOPLOSS,Point.round);else if(sl=="")sl="N";
return_value(str,"tp=",tp);
if(tp==""&&LO_AUTO_INCLUDE_SL_TP==1)tp=DoubleToStr(LO_PIPPROFIT,Point.round);else if(tp=="")tp="N";
return_value(str,"lo=",lo);
if(lo=="")lo=DoubleToStr(LO_LOTS,1);
return_value(str,"ts=",ts);
if(ts=="")ts=DoubleToStr(LO_PIPTRAIL,Point.round);
return_value(str,"sq=",sq);
if((StringFind(name,LO_PREFIX+"buysl")>-1||StringFind(name,LO_PREFIX+"sellsl")>-1)&&sq==""){
sq = DoubleToStr(ObjectGet(name,OBJPROP_PRICE1),Digits);
}
return_value(str,"tq=",tq);
if((StringFind(name,LO_PREFIX+"buytp")>-1||StringFind(name,LO_PREFIX+"selltp")>-1)&&tq==""){
tq = DoubleToStr(ObjectGet(name,OBJPROP_PRICE1),Digits);
}
int command,exp,MAGIC_NO;
double type;
if(MAGIC_NUMBER<0)MAGIC_NO=0;else MAGIC_NO=MAGIC_NUMBER;
if(StringFind(name,LO_PREFIX+"buypend")>-1||StringFind(name,LO_PREFIX+"sellpend")>-1){
if(StringFind(name,LO_PREFIX+"buypend")>-1){
if(ObjectGet(name,OBJPROP_PRICE1)>MarketInfo(Symbol(),MODE_ASK)){command = OP_BUYSTOP;type = NormalizeDouble(ObjectGet(name,OBJPROP_PRICE1),Digits);exp=0;}else {command = OP_BUYLIMIT;type = NormalizeDouble(ObjectGet(name,OBJPROP_PRICE1),Digits);exp=0;}
}
if(StringFind(name,LO_PREFIX+"sellpend")>-1){
if(ObjectGet(name,OBJPROP_PRICE1)<MarketInfo(Symbol(),MODE_BID)){command = OP_SELLSTOP;type = NormalizeDouble(ObjectGet(name,OBJPROP_PRICE1),Digits);exp=0;}else {command = OP_SELLLIMIT;type = NormalizeDouble(ObjectGet(name,OBJPROP_PRICE1),Digits);exp=0;}

}
}else if(StringFind(name,LO_PREFIX+"buy")>-1){command = OP_BUY;type = MarketInfo(Symbol(),MODE_ASK);exp=0;}else {command=OP_SELL;type = MarketInfo(Symbol(),MODE_BID);exp=0;}
if(sq==""||LO_ECN==1)sq="0";
if(tq==""||LO_ECN==1)tq="0";
int ticket = OrderSend(Symbol(),command,StrToDouble(lo), type, MarketInfo(Symbol(),MODE_SPREAD), /*StrToDouble(sq)*/0, /*StrToDouble(tq)*/0,"", MAGIC_NO, exp);
if(ticket>-1){
OrderSelect(ticket,SELECT_BY_TICKET);
if(StringFind(name,LO_PREFIX+"buytp")>-1)tp=DoubleToStr((StrToDouble(tq)-OrderOpenPrice())/Point.pip,Point.round);else if(StringFind(name,LO_PREFIX+"selltp")>-1)tp=DoubleToStr((OrderOpenPrice()-StrToDouble(tq))/Point.pip,Point.round);
if(StringFind(name,LO_PREFIX+"sellsl")>-1)sl=DoubleToStr((StrToDouble(sq)-OrderOpenPrice())/Point.pip,Point.round);else if(StringFind(name,LO_PREFIX+"buysl")>-1)sl=DoubleToStr((OrderOpenPrice()-StrToDouble(sq))/Point.pip,Point.round);
if(sl!=""&&sq=="0")if(command==OP_BUY||command==OP_BUYSTOP||command==OP_BUYLIMIT)sq=DoubleToStr(OrderOpenPrice()-(StrToDouble(sl)*Point.pip),Digits);else if(command==OP_SELL||command==OP_SELLSTOP||command==OP_SELLLIMIT)sq=DoubleToStr(OrderOpenPrice()+(StrToDouble(sl)*Point.pip),Digits);
if(tp!=""&&tq=="0"&&StrToDouble(tp)==0){if(command==OP_BUY||command==OP_BUYSTOP||command==OP_BUYLIMIT)tq=DoubleToStr(mainString(tp),Digits);}else if(tp!=""&&tq=="0"){if(command==OP_BUY||command==OP_BUYSTOP||command==OP_BUYLIMIT)tq=DoubleToStr(OrderOpenPrice()+(StrToDouble(tp)*Point.pip),Digits);else if(command==OP_SELL||command==OP_SELLSTOP||command==OP_SELLLIMIT)tq=DoubleToStr(OrderOpenPrice()-(StrToDouble(tp)*Point.pip),Digits);}
if(!OrderModify(ticket,OrderOpenPrice(),StrToDouble(sq),StrToDouble(tq),OrderExpiration())){
Print("Order not modified "+ErrorDescription(GetLastError())+" sq:"+sq+" tq:"+tq);
}
string text = " sl="+sl+" tp="+tp+" ts="+ts+" lo="+lo;
ObjectCreate(LO_PREFIX+ticket, OBJ_HLINE, 0, 0, OrderOpenPrice());
ObjectSetText(LO_PREFIX+ticket,text);
ObjectSet(LO_PREFIX+ticket,OBJPROP_COLOR, Gray);
ObjectSet(LO_PREFIX+ticket,OBJPROP_STYLE, STYLE_DASH);
GlobalVariableSet(MLO_PREFIX+ticket,1);
return(1);
}
return (0);
}