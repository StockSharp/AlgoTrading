//+------------------------------------------------------------------+
//|                                                    buy_order.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//#include <HPCS_libfunctions_include.mqh>
bool check = true;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int count = 0;
int OnInit()
 {
 
 EventSetTimer(1);
 
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
 /* while(CloseAllMarketOrders(16772,20)==0)
  {
   Print("calling Sleep______________");
   Sleep(20);
  }*/
  
  }
//+------------------------------------------------------------------+
void OnTimer()
{
 if(TimeSeconds(TimeCurrent())==count)
 { 
 string timesec = IntegerToString(TimeSeconds(TimeCurrent()));
 int ticket = OrderSend(Symbol(),OP_BUY,0.01,Ask,3,0,0,timesec,16772,0,clrGreen);
 if(ticket>0)
 Print("Buy Order Placed");
 else
 Print("OrderSend failed, error#",GetLastError());
 count++;
 if(count>59)
 ExpertRemove();
 }
}