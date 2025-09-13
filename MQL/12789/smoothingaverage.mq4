//+------------------------------------------------------------------+
//|                                             SmoothingAverage.mq4 |
//|                                                       Rietsuiker |
//|                                                     http://ak.fm |
//+------------------------------------------------------------------+
#property copyright "Rietsuiker"
#property link      "http://ak.fm"
#property version   "0.01"

//--- input parameters
input int      Period_MA=200;
input double      Risk=10;
input int      Smoothing=1400;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int CurrentOrder;

double MA;
double MA2;
double highpoint;

double Lots=(AccountEquity()*Point*Risk);
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   Print("You're gonna get bankrupt m8");
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Print(AccountEquity());
   Print(Lots);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

   MA=iMA(NULL,0,Period_MA,0,MODE_SMA,PRICE_CLOSE,0);

   if(OrdersTotal()<1)
     {

      if(Bid<MA+Smoothing*Point)
        {;
         OrderSend(Symbol(),OP_SELL,Lots,Bid,10,0,0);
         CurrentOrder++;
           } else if(Ask>MA-Smoothing*Point){
         OrderSend(Symbol(),OP_BUY,Lots,Ask,10,0,0);
         CurrentOrder++;
        }

      OrderSelect(CurrentOrder,SELECT_BY_TICKET);
     }

   if(OrderType()==OP_SELL && Bid>MA+Smoothing*Point)
     {
      OrderClose(CurrentOrder,Lots,Ask,3,Red);
        } else if(OrderType()==OP_BUY && Ask<MA-Smoothing*Point){
      OrderClose(CurrentOrder,Lots,Bid,3,Red);
     }

  }
//+------------------------------------------------------------------+
