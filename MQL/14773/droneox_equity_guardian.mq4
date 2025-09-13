//+------------------------------------------------------------------+
//|                                               Equity Guardian    |
//|                              Copyright © 2015, Yogie  Pratama    |
//|                                        droneox01@gmail.com       |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Yogie Pratama"
#property link      "droneox01@gmail.com"
#property version   "1.00"
#property strict

#include <WinUser32.mqh>

extern string EAName="Equity Guardian";
extern string Copyright="Droneox";
extern bool CloseOrder=TRUE; //Close All Order
extern bool disableexpert=TRUE; //Disable Expert Advisor
extern double EquityTarget=999999; //Equity Take Profit (USD)
extern double EquityStop=0; //Equity Stop Loss (USD)
//+------------------------------------------------------------------+
//| check live trading and dll                                       |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!IsTradeAllowed() && !IsDllsAllowed())
     {
      Alert("Please Allow Live Trading and DLL Import");
      return(1);
     }
   else if(!IsTradeAllowed())
     {
      Alert("Please Allow Live Trading");
      return(1);
     }
   else if(!IsDllsAllowed())
     {
      Alert("Please Allow DLL Import");
      return(1);
     }
   else
     {
      return(0);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double equity=AccountEquity();
   double accbalance=AccountBalance();

   int total=OrdersTotal();

   if(IsExpertEnabled())
     {
      if(equity<=EquityStop)
        {
         if(total!=0 && CloseOrder==TRUE)
           {
            CloseAllTrade();
           }
         if(disableexpert)
           {
            DisableEA();
           }
         Print("Equity Guardian reach equity stop level");
        }

      if(equity>=EquityTarget)
        {
         if(total!=0 && CloseOrder==TRUE)
           {
            CloseAllTrade();
           }
         if(disableexpert)
           {
            DisableEA();
           }
         Print("Equity Guardian reach equity Target level");
        }
     }
   return(0);
  }
//disable autotrading
void DisableEA() 
  {
   keybd_event(17,0,0,0);
   keybd_event(69,0,0,0);
   keybd_event(69,0,2,0);
   keybd_event(17,0,2,0);
  }
//close all open trade
int CloseAllTrade() 
  {
   int total=OrdersTotal();
   int t;
   int cnt=0;
   for(cnt=0; cnt<=total; cnt++)
     {
      bool s=OrderSelect(0,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()==OP_BUY)
         t=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),5,Violet);
      if(OrderType()==OP_SELL)
         t=OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),5,Violet);
      if(OrderType()>OP_SELL) //pending orders
         t=OrderDelete(OrderTicket());
     }
   return(0);
  }
//+------------------------------------------------------------------+
