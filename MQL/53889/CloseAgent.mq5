//+------------------------------------------------------------------+
//|                                                   CloseAgent.mq5 |
//+------------------------------------------------------------------+
#property copyright   "Free example for the community."
#property description "Optimal closing tool with Bollinger Bands and RSI."
#property version     "1.00"
#property strict
//+--- Classes ------------------------------------------------------!
#include <Trade\Trade.mqh> CTrade trade;
//+--- Definitions --------------------------------------------------!
enum MCLOSE{
   Manual=0, // Manual trades
   Auto=1,   // Algo trades
   Both=2,};
enum OPMODE{LiveBar=0,NewBar=1};
//+--- Parameters ---------------------------------------------------!
input MCLOSE          CloseMode      = 2;
input ENUM_TIMEFRAMES WTimeframe     = PERIOD_M5;
input OPMODE          OperationMode  = 0;
input double          CloseAll       = 0.0;   // Close all at profit (0 disabled)
input bool            EnableAlerts   = true;
//+------------------------------------------------------------------!
double UBB[],LBB[],RSI[];
const int FastMA=13,SlowMA=21;
double acp=AccountInfoDouble(ACCOUNT_PROFIT);
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){EventSetTimer(1);
   ArraySetAsSeries(UBB,true);
   ArraySetAsSeries(LBB,true);
   ArraySetAsSeries(RSI,true);
   return(INIT_SUCCEEDED);}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){EventKillTimer();}
//+------------------------------------------------------------------+
//| My algorithm function                                            |
//+------------------------------------------------------------------+
void MyAlgo(){
   int opmod=OperationMode,
   total=PositionsTotal()-1;
   for(int c=total;c>=0;c--){
      ulong Ticket=PositionGetTicket(c);
      long Magic=PositionGetInteger(POSITION_MAGIC);
      string SName=PositionGetString(POSITION_SYMBOL);
      int RSIh=iRSI(SName,WTimeframe,FastMA,PRICE_CLOSE),
      BBSh=iBands(SName,WTimeframe,SlowMA,0,2,PRICE_CLOSE);
      if(CopyBuffer(RSIh,0,opmod,FastMA,RSI)<=0){
         Print("RSI CopyBuffer error in ",SName);
         continue;} // Skip errors and continue.
      else if(CopyBuffer(BBSh,1,opmod,SlowMA,UBB)<=0){
         Print("UBB CopyBuffer error in ",SName);
         continue;} // Skip errors and continue.
      else if(CopyBuffer(BBSh,2,opmod,SlowMA,LBB)<=0){
         Print("LBB CopyBuffer error in ",SName);
         continue;} // Skip errors and continue.
      if(CloseMode>1||(CloseMode==1&&Magic>0)||(CloseMode<1&&Magic==0)){
         double Low=iLow(SName,PERIOD_D1,0);
         double High=iHigh(SName,PERIOD_D1,0);
         double Close=iClose(SName,PERIOD_D1,0),
         PosProfit=PositionGetDouble(POSITION_PROFIT);
         double Ask=SymbolInfoDouble(SName,SYMBOL_ASK);
         double Bid=SymbolInfoDouble(SName,SYMBOL_BID),
         PosOpen=PositionGetDouble(POSITION_PRICE_OPEN);
         if(CloseAll>0&&acp>CloseAll){
            if(EnableAlerts){
               Alert("Closing all positions at ",DoubleToString(acp,2)," profit.");}
            if(!trade.PositionClose(Ticket,ULONG_MAX)){
               Print("PositionClose error ",trade.ResultRetcode());
               continue; // Skip errors and continue.
               }
            }
         else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY){
            if(Bid>UBB[opmod]&&RSI[opmod]>70&&Bid>PosOpen){
               if(EnableAlerts){
                  Alert("Closing ",SName," at ",DoubleToString(PosProfit,2)," profit.");}
               if(!trade.PositionClose(Ticket,ULONG_MAX)){
                  Print("PositionClose error ",trade.ResultRetcode());
                  continue; // Skip errors and continue.
                  }
               }
            }
         else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL){
            if(Ask<LBB[opmod]&&RSI[opmod]<30&&Ask<PosOpen){
               if(EnableAlerts){
                  Alert("Closing ",SName," at ",DoubleToString(PosProfit,2)," profit.");}
               if(!trade.PositionClose(Ticket,ULONG_MAX)){
                  Print("PositionClose error ",trade.ResultRetcode());
                  continue; // Skip errors and continue.
                  }
               }
            }
         }
      }
   }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer(){MyAlgo();}
//+--------------------------------------------------------- End. ---+