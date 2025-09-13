//+------------------------------------------------------------------+
//|                              Donchain counter-channel system.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "by Michal Rutka"
#property link      ""
/*
  System 'Donchain counter-channel' was described in the Currency Trader
  Magazine, Octobre 2005, p.32.
  
  Rules:
    1. Go long next day at market if the 20-day
       lower Donchian channel line turns up.
    2. Go short next day at market if the upper 20-day
       Donchian channel line turns down.
    3. Exit long with a stop at the lower 20-day Donchian
       channel line.
    4. Exit short with a stop at the upper 20-day
       Donchian channel line.
       
  Strategy uses a custom indicator "Donchain Channels - Generalized version.mq4", which must be put in the indicators 
  subfolder.

    Version     Date         Comment
    1.0         9 Oct 2005   First version based on the article from the Currency Trader magazine.
*/
extern double Lots    =1.0;                  // MM will come when strategy proved 
extern int    Slippage=3;                    // Change it for your brooker
extern int    Magic   =20051006;             // Magic number of the strategy
// Optimalization parameters:
extern int    ChannelPeriod=20;              // Original channel period from th article.
extern int    TimeFrame    =PERIOD_D1;       // Time frame of the Donchain indicator.
// Privete variables
datetime last_trade_time;                    // in order to execute maximum only one trade a day.
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//---- 
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
//|                                                                  |
//+------------------------------------------------------------------+
void ReportStrategy(int Magic)
  {
   int    totalorders=HistoryTotal();
   double StrategyProfit=0.0;
   int    StrategyOrders=0;
   for(int j=0; j<totalorders;j++)
     { if(OrderSelect(j, SELECT_BY_POS, MODE_HISTORY) &&
            (OrderMagicNumber()==Magic))
        {
         if((OrderType()==OP_BUY) ||
            (OrderType()==OP_SELL))
           {
            StrategyOrders++;
            StrategyProfit+=OrderProfit();
           }
        }
     }
   totalorders=OrdersTotal();
   for(j=0; j<totalorders;j++)
     { 
     if(OrderSelect(j, SELECT_BY_POS, MODE_TRADES) &&
            (OrderMagicNumber()==Magic))
        {
         if((OrderType()==OP_BUY) ||
            (OrderType()==OP_SELL))
           {
            StrategyOrders++;
            StrategyProfit+=OrderProfit();
           }
        }
     }
   Comment("Executed ", StrategyOrders, " trades with ", StrategyProfit," of profit");
   return;
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   double stop_short;
   double stop_long;
//----
   bool entry_short;
   bool entry_long;
   bool are_we_long    =false;
   bool are_we_short   =false;
   int  orders_in_trade=0;
   int  active_order_ticket;
//---- 
   ReportStrategy(Magic);
   // Check current trades:
   int TotalOrders=OrdersTotal();
     for(int j=0;j<TotalOrders;j++)
     {
      OrderSelect(j, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol() &&
             OrderMagicNumber()==Magic)
             {
         are_we_long =are_we_long  || OrderType()==OP_BUY;
         are_we_short=are_we_short || OrderType()==OP_SELL;
         orders_in_trade++;
         active_order_ticket=OrderTicket();
        }
     }
     if(orders_in_trade > 1)
     {
      Alert("More than one active trade! Please close the wrong one.");
      return(-1);
     }
   // Lower channel:
   stop_long=iCustom(NULL, TimeFrame, "Donchian Channels - Generalized version1",ChannelPeriod,1,0,0,0,0,0);
   // Upper channel:
   stop_short=iCustom(NULL, TimeFrame, "Donchian Channels - Generalized version1",ChannelPeriod,1,0,0,0,1,0);
     if(are_we_long)
     {
      // We have long position. Check stop loss:
      OrderSelect(active_order_ticket, SELECT_BY_TICKET);
      if(OrderStopLoss() < stop_long)
         OrderModify(active_order_ticket,
                     OrderOpenPrice(),
                     stop_long,
                     OrderTakeProfit(),
                     0,
                     Blue);
//----
      return(0);
     }
     if(are_we_short)
     {
      // We have long position. Check stop loss:
      OrderSelect(active_order_ticket, SELECT_BY_TICKET);
      if(OrderStopLoss() > stop_short)
         OrderModify(active_order_ticket,
                     OrderOpenPrice(),
                     stop_short,
                     OrderTakeProfit(),
                     0,
                     Blue);
//----
      return(0);
     }
   // Do not execute new trade for a next 24 hours.
   if((CurTime() - last_trade_time)<24*60*60) return(0);
   // Lower channel:
   entry_long=iCustom(NULL, TimeFrame, "Donchian Channels - Generalized version1",ChannelPeriod,1,0,0,0,0,1) >
   iCustom(NULL, TimeFrame, "Donchian Channels - Generalized version1",ChannelPeriod,1,0,0,0,0,2);
   // Upper channel:
   entry_short=iCustom(NULL, TimeFrame, "Donchian Channels - Generalized version1",ChannelPeriod,1,0,0,0,1,1) <
   iCustom(NULL, TimeFrame, "Donchian Channels - Generalized version1",ChannelPeriod,1,0,0,0,1,2);
   if(entry_long && entry_short)
      Alert("Short and long entry. Probably one of the is wrong.");
     if(entry_long)
     {
      OrderSend(Symbol(),
                OP_BUY,
                Lots,
                Ask,
                Slippage,
                stop_long,
                0,
                "",
                Magic,
                0,
                FireBrick);
      last_trade_time=CurTime();
     }
     if(entry_short)
     {
      OrderSend(Symbol(),
                OP_SELL,
                Lots,
                Bid,
                Slippage,
                stop_short,
                0,
                "",
                Magic,
                0,
                FireBrick);
      last_trade_time=CurTime();
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+