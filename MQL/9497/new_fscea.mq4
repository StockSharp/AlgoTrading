//+------------------------------------------------------------------+
//|                                                    new fscea.mq4 |
//|                                                        brayt eze |
//|                                               braytone@gmail.com |
//+------------------------------------------------------------------+
#property copyright "brayt"
#property link      "braytone@gmail.com"


extern double TakeProfit    = 300;
extern double Lots          = 0.1;
extern double TrailingStop  = 20;
extern double OpenLevel     = 3;
extern double CloseLevel    = 2;
extern double TrendPeriod   = 10;
extern double TrendShift    = 2;

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double Ma;
   double MacdCurrent, MacdPrevious, SignalCurrent;
   double SignalPrevious, MaCurrent, MaPrevious;
   int cnt, ticket, total;


   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);  
     }
   if(TakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0);
     }


   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
   MaCurrent=iMA(NULL,0,TrendPeriod,TrendShift,MODE_EMA,PRICE_CLOSE,0);
   MaPrevious=iMA(NULL,0,TrendPeriod,TrendShift,MODE_EMA,PRICE_CLOSE,1);

   total=OrdersTotal();
   if(total<1) 
     {
      
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }
      
      if(MacdCurrent<0 && MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious &&
         MathAbs(MacdCurrent)>(OpenLevel*Point) && MaCurrent>MaPrevious)
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,Ask+TakeProfit*Point,"new fscea",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
        }
      
      if(MacdCurrent>0 && MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious && 
         MacdCurrent>(OpenLevel*Point) && MaCurrent<MaPrevious)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,Bid-TakeProfit*Point,"new fscea",16384,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
           }
         else Print("Error opening SELL order : ",GetLastError()); 
         return(0); 
        }
      return(0);
     }

   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&
         OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)
           {

            if(MacdCurrent>0 && MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious &&
               MacdCurrent>(CloseLevel*Point))
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);
                 return(0);
                }

            if(TrailingStop>0)  
              {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                     return(0);
                    }
                 }
              }
           }
         else
           {

            if(MacdCurrent<0 && MacdCurrent>SignalCurrent &&
               MacdPrevious<SignalPrevious && MathAbs(MacdCurrent)>(CloseLevel*Point))
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);
               return(0);
              }

            if(TrailingStop>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }
           }
        }
     }
   return(0);
  }