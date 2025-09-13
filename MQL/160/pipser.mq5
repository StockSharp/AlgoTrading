//+------------------------------------------------------------------+
//|                                                       Pipser.mq5 |
//|                                             Copyright 2010, Alf. |
//|        http://forum.liteforex.org/showthread.php?p=6432#post6432 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Alf."
#property link      "http://forum.liteforex.org/showthread.php?p=6432#post6432"
#property version   "1.00"
//--- input parameters
input double   Lot=0.1;
input int      SL=150;
input int      Slipage=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   ObjectCreate(0,"BUY",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"BUY",OBJPROP_XDISTANCE,ChartGetInteger(0,CHART_WIDTH_IN_PIXELS)-100);
   ObjectSetInteger(0,"BUY",OBJPROP_YDISTANCE,50);
   ObjectSetString(0,"BUY",OBJPROP_TEXT,"Buy");
   ObjectCreate(0,"SELL",OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,"SELL",OBJPROP_XDISTANCE,ChartGetInteger(0,CHART_WIDTH_IN_PIXELS)-100);
   ObjectSetInteger(0,"SELL",OBJPROP_YDISTANCE,80);
   ObjectSetString(0,"SELL",OBJPROP_TEXT,"Sell");
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectDelete(0,"BUY");
   ObjectDelete(0,"SELL");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
  }
//+------------------------------------------------------------------+
//| OnChart event handler                                            |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam)
  {
   double SL2=SL;
   if(_Digits==5 || _Digits==3)  SL2=SL2*10;

   MqlTick last_tick;
   SymbolInfoTick(_Symbol,last_tick);
   MqlTradeResult result;
   MqlTradeRequest request;
   ZeroMemory(request);
   ZeroMemory(result);

   request.symbol=_Symbol;
   request.magic=777;
   request.deviation=Slipage;
   request.action=TRADE_ACTION_DEAL;
   request.type_filling=ORDER_FILLING_FOK;
   if(ObjectGetInteger(0,"BUY",OBJPROP_STATE)!=0)
     {
      ObjectSetInteger(0,"BUY",OBJPROP_STATE,0);
      request.volume=Lot;
      request.price=last_tick.ask;
      request.type=ORDER_TYPE_BUY;
      if(SL2!=0) request.sl=last_tick.bid-SL2*Point();
      else request.sl=0;
      request.tp=0;
      OrderSend(request,result);
      return;
     }
   if(ObjectGetInteger(0,"SELL",OBJPROP_STATE)!=0)
     {
      ObjectSetInteger(0,"SELL",OBJPROP_STATE,0);
      request.volume=Lot;
      request.price=last_tick.bid;
      request.type=ORDER_TYPE_SELL;
      if(SL2!=0) request.sl=last_tick.ask+SL2*Point();
      else request.sl=0;
      request.tp=0;
      OrderSend(request,result);
      return;
     }
  }
//+------------------------------------------------------------------+
