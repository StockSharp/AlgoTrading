//+------------------------------------------------------------------+
//|                                          Contrarian_trade_MA.mq4 |
//|                                                            Seiji |
//+------------------------------------------------------------------+
extern double Lots=0.5;
extern double CalcPeriod=4;
extern double MAPeriod=7;
extern double StopLoss=300;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double MA7, Open0, Max, Min, Close1;
   int cnt, ticket, total;
   // initial data checks
   // it is important to make sure that the expert works with a normal
   // chart and the user did not make any mistakes setting external 
   // variables (Lots, StopLoss, TakeProfit, 
   // TrailingStop) in our case, we check TakeProfit
   // on a chart of less than 100 bars
   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }
   // to simplify the coding and speed up access
   // data are put into internal variables
   MA7=iMA(NULL,PERIOD_W1,MAPeriod,0,MODE_SMA,PRICE_CLOSE,1);
   Open0=iOpen(NULL,PERIOD_W1,0);
   Max=iHigh(NULL,PERIOD_W1,Highest(NULL,PERIOD_W1,MODE_HIGH,CalcPeriod,2));
   Min=iLow(NULL,PERIOD_W1,Lowest(NULL,PERIOD_W1,MODE_LOW,CalcPeriod,2));
   Close1=iClose(NULL,PERIOD_W1,1);
   //
   total=OrdersTotal();
   if(total<1)
     {
      // no opened orders identified
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);
        }
      // check for long position (BUY) possibility
      if(DayOfWeek()==1)
        {
         if(Max<Close1)
           {
            ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,0,"macd sample",16384,0,Green);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
              }
            else Print("Error opening BUY order : ",GetLastError());
            return(0);
           }
        }
      if(DayOfWeek()==1)
        {
         if(Min>Close1)
           {
            ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*Point,0,"macd sample",16384,0,Red);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
              }
            else Print("Error opening SELL order : ",GetLastError());
            return(0);
           }
        }
      if(DayOfWeek()==1)
        {
         if(MA7>Open0)
           {
            ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,0,"macd sample",16384,0,Green);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
              }
            else Print("Error opening BUY order : ",GetLastError());
            return(0);
           }
        }
      // check for short position (SELL) possibility
      if(DayOfWeek()==1)
        {
         if(MA7<Open0)
           {
            ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*Point,0,"macd sample",16384,0,Red);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
              }
            else Print("Error opening SELL order : ",GetLastError());
            return(0);
           }
         return(0);
        }
     }
   // it is important to enter the market correctly, 
   // but it is more important to exit it correctly...   
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&   // check for opened position 
         OrderSymbol()==Symbol())  // check for symbol
        {
         if(OrderType()==OP_BUY)   // long position is opened
           {
            // should it be closed?
            if(CurTime()-OrderOpenTime()>604800)
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
               return(0); // exit
              }
           }
         else // go to short position
           {
            // should it be closed?
            if(CurTime()-OrderOpenTime()>604800)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close position
               return(0); // exit
              }
           }
        }
     }
   return(0);
  }
// the end.
//+------------------------------------------------------------------+