//+------------------------------------------------------------------+
//|                                                       GBP9AM.mq4 |
//|                                                      forxexpoolo |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "forxexpoolo, coded by Michal Rutka"
#property link      ""
/* 
   Attach to GBPUSD chart. Timeframe is not important. System should put 
   trades on 9AM London time.

   I looking for system automat wich will be trade for me this

   9:00 GMT look for price GBP/USD 

   then price is 1.8230example (bid)

   Robot do for me order waiting position 
   long and short 

   open long 18pips+4pips spread at this price so will be 

   LONG 1.8252 profit 40pips   stop 22pips 

   And short 

   open short 22pips-4pipsspread so
   1.8230-22-4=1.8204 
   so 1.8204 short profit 40pips stop 18pips 

   AND MAJOR IF I OPEN LONG POSITION AUTOMAT CANCEL SHORT POSITION 

   This system work nice month week day ?
   I will be glad if I see this in .mq4 code 

   Version:    Date:       Comment:
   --------------------------------
   1.0         2005.10.16  First version according to the idea.
   1.1         2005.10.17  Bug removed (closing active orders when fired at the
                          look hour and issuing new wrong pair of orders).
                          Added close hour, request of Movieweb.
   1.2         2005.10.18  Bug removed (allowing more than one trades per day, e.g. 2005.07.20)
   1.3         2005.10.19  Feature added: look_price_min.
*/
extern int look_price_hour=10;   // Change for your time zone (my is +1 Hour). Should be 9AM London time.
extern int look_price_min =0;    // Offset in minutes when to look on price.
extern int close_hour     =18;   // Close all orders after this hour
extern bool use_close_hour=true; // set it to false to ignore close_hour
extern int take_profit    =40;
extern int open_long      =18;
extern int open_short     =22;
extern int stop_long      =22;
extern int stop_short     =18;
extern int slippage       =0;   // Put what your brooker requires
extern double lots        =0.1; // Position size
extern int magic          =20051016;
//----
bool clear_to_send        =true;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ReportStrategy()
  {
   int    totalorders       =HistoryTotal();
   double StrategyProfit    =0.0;
   double StrategyProfitOpen=0.0;
   int    StrategyOrders    =0;
   int    StrategyOrdersOpen=0;
   for(int j=0; j<totalorders;j++)
     { if(OrderSelect(j, SELECT_BY_POS, MODE_HISTORY) &&
            (OrderMagicNumber()==magic))
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
     { if(OrderSelect(j, SELECT_BY_POS, MODE_TRADES) &&
            (OrderMagicNumber()==magic))
        {
         if((OrderType()==OP_BUY) ||
            (OrderType()==OP_SELL))
           {
            StrategyOrdersOpen++;
            StrategyProfitOpen+=OrderProfit();
           }
        }
     }
   Comment("Executed ", StrategyOrders,"+",StrategyOrdersOpen, " trades with ", StrategyProfit,"+",
           StrategyProfitOpen," = ",StrategyProfit+StrategyProfitOpen," of profit\n",
           "Server hour: ", TimeHour(CurTime()), " Local hour: ", TimeHour(LocalTime()));
   return;
  }
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//---- 
   ReportStrategy();
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
   ReportStrategy();
//----
   if(Hour()>=close_hour &&
          use_close_hour)
          {
      // we are after closing time
      int totalorders=OrdersTotal();
        for(int j=0;j<totalorders;j++)
        {
         OrderSelect(j, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==Symbol() &&
                OrderMagicNumber()==magic)
                {
            if(OrderType()==OP_BUY)
               OrderClose(OrderTicket(), OrderLots(), Bid, 0, Red);
            if(OrderType()==OP_SELL)
               OrderClose(OrderTicket(), OrderLots(), Ask, 0, Red);
            if(OrderType()==OP_BUYSTOP  || OrderType()==OP_SELLSTOP)
               OrderDelete(OrderTicket());
           }
        }
      return(0);
     }
   if(Hour()==look_price_hour &&
      Minute()>=look_price_min &&
          clear_to_send)
          {
      // Probably I need to close any old positions first:
      totalorders=OrdersTotal();
        for(j=0;j<totalorders;j++)
        {
         OrderSelect(j, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==Symbol() &&
                OrderMagicNumber()==magic)
                {
            if(OrderType()==OP_BUY)
               OrderClose(OrderTicket(), OrderLots(), Bid, 0, Red);
            if(OrderType()==OP_SELL)
               OrderClose(OrderTicket(), OrderLots(), Ask, 0, Red);
            if(OrderType()==OP_BUYSTOP  || OrderType()==OP_SELLSTOP)
               OrderDelete(OrderTicket());
           }
        }
      // Send orders:
      OrderSend(Symbol(),
                OP_BUYSTOP,
                lots,
                Ask+open_long*Point, // Spread included
                slippage,
                Bid+(open_long-stop_long)*Point,
                Bid+(open_long+take_profit)*Point,
                NULL,
                magic,
                0,
                FireBrick);
      OrderSend(Symbol(),
                OP_SELLSTOP,
                lots,
                Bid-open_short*Point,
                slippage,
                Ask-(open_short-stop_short)*Point,
                Ask-(open_short+take_profit)*Point,
                NULL,
                magic,
                0,
                FireBrick);
      clear_to_send=false; // mark that orders are sent
     }
     if(!clear_to_send)
     {  // there are active orders
      int long_ticket =-1;
      int short_ticket=-1;
      bool no_active_order=true;
      totalorders=OrdersTotal();
        for(j=0;j<totalorders;j++)
        {
         OrderSelect(j, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==Symbol() &&
                OrderMagicNumber()==magic)
                {
            if(OrderType()==OP_BUYSTOP)
               long_ticket=OrderTicket();
            if(OrderType()==OP_SELLSTOP)
               short_ticket=OrderTicket();
            if(OrderType()==OP_BUY ||
               OrderType()==OP_SELL) // Active order
               no_active_order=false;
           }
        }
      if(short_ticket==-1 && long_ticket!=-1)
         OrderDelete(long_ticket);
      if(long_ticket==-1 && short_ticket!=-1)
         OrderDelete(short_ticket);
      if(long_ticket==-1 && short_ticket==-1 && no_active_order &&
         Hour()!=look_price_hour && Minute()>=look_price_min)
         clear_to_send=true;
      if(Hour()==(look_price_hour-1) &&
        MathAbs(Minute() - look_price_min) < 10)
         clear_to_send=true;
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+