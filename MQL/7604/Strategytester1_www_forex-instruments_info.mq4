//+------------------------------------------------------------------+
//|                                           StrategyTester Ea .mq4 |
//|                             Copyright © 2006, dr_richard_gaines  |
//|                          Modified by Ing. Ronald Verwer/ROVERCOM |
//|   No liability for accuracy of operation of this EA is accepted. |
//|                                                      25 May 2006 |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, dr richard gaines"
#property link      "dr_richard_gaines@yahoo.com"
//----
#define MAGICMA  481429
//----
extern double TakeProfit=10;
extern double TrailingStop=5;
extern double StopLoss=50;
extern double Lots=0.1;
// Use MoneyManagement = 9; this is just a place holder
// extern double SecureProfit = spread;
// extern double Whatever you want = $$$;
// Your indicators externs and user inputs go here.
//MACD
extern double MACDOpenLevel=3;
extern double MACDCloseLevel=2;
extern double MATrendPeriod=26;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   //MACD   
   double MacdCurrent, MacdPrevious, SignalCurrent;
   double SignalPrevious, MaCurrent, MaPrevious;
   // Your indicator is substituted   
   double yourindicatorGreen;
   double yourindicatorRed;
   int cnt, ticket, total;
   // Your indicators iCustom or iName is called here and goes here.
   yourindicatorGreen=iCustom(NULL, 0, "yourindicator",15,3,3, 0, 1);
   yourindicatorRed=iCustom(NULL, 0, "yourindicator",15,3,3, 1, 1);
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
   if(TakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0);  // check TakeProfit
     }
   // to simplify the coding and speed up access
   // data are put into internal variables
   // Your indicators iCustom or iName call goes here
   yourindicatorGreen=iCustom(NULL, 0, "yourindicator",15,3,3, 0, 1);
   yourindicatorRed=iCustom(NULL, 0, "yourindicator",15,3,3, 1, 1);
   //MACD   
   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
   MaCurrent=iMA(NULL,0,MATrendPeriod,0,MODE_EMA,PRICE_CLOSE,0);
   MaPrevious=iMA(NULL,0,MATrendPeriod,0,MODE_EMA,PRICE_CLOSE,1);
//----
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
      if((yourindicatorGreen>yourindicatorRed))
        {
         //MACD      
         if(MacdCurrent<0 && MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious &&
            MathAbs(MacdCurrent)>(MACDOpenLevel*Point) && MaCurrent>MaPrevious)
           {
            ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"Strategy Buy",16384,0,Green);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                  Print("BUY order opened : ",OrderOpenPrice());
              }
            else Print("Error opening BUY order : ",GetLastError());
            return(0);
           }
        }
      // check for short position (SELL) possibility
      if((yourindicatorGreen<yourindicatorRed))
        {
         //MACD      
         if(MacdCurrent>0 && MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious &&
            MacdCurrent>(MACDOpenLevel*Point) && MaCurrent<MaPrevious)
           {
            ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid+StopLoss,3,0,Bid-TakeProfit*Point,"Strategy Sell",16384,0,Red);
            if(ticket>0)
              {
               if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
                  Print("SELL order opened : ",OrderOpenPrice());
              }
            else Print("Error opening SELL order : ",GetLastError());
            return(0);
           }
         return(0);
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
               if((yourindicatorGreen<yourindicatorRed))
                 {
                  //MACD            
                  if(MacdCurrent>0 && MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious &&
                     MacdCurrent>(MACDCloseLevel*Point))
                    {
                     OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
                     return(0); // exit
                    }
                 }
               // check for trailing stop
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
            else // go to short position
              {
               // should it be closed?
               if((yourindicatorGreen>yourindicatorRed))
                 {
                  //MACD            
                  if(MacdCurrent<0 && MacdCurrent>SignalCurrent &&
                     MacdPrevious<SignalPrevious && MathAbs(MacdCurrent)>(MACDCloseLevel*Point))
                    {
                     OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close position
                     return(0); // exit
                    }
                 }
               // check for trailing stop
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
     }
   return(0);
  }
// the end.
//+------------------------------------------------------------------+