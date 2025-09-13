//+------------------------------------------------------------------+
//|                           Copyright 2005, Gordago Software Corp. |
//|                                          http://www.gordago.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2005, Gordago Software Corp."
#property link      "http://www.gordago.com"
//----
extern double lStopLoss=17;
extern double sStopLoss=46;
extern double lTrailingStop=18;
extern double sTrailingStop=22;
extern double Lots=0.1;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int start()
  {
   int cnt, ticket;
     if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }
     if(lStopLoss<10)
     {
      Print("StopLoss less than 10");
      return(0);
     }
     if(sStopLoss<10)
     {
      Print("StopLoss less than 10");
      return(0);
     }
   double diMACD0=iMACD(NULL,60,13,30,9,PRICE_CLOSE,MODE_MAIN,0);
   double diMACD1=iMACD(NULL,60,13,30,9,PRICE_CLOSE,MODE_MAIN,1);
   double diMACD2=iMACD(NULL,60,13,30,9,PRICE_CLOSE,MODE_MAIN,1);
   double diStochastic3=iStochastic(NULL,15,2,3,3,MODE_EMA,PRICE_CLOSE,MODE_MAIN,0);
   double d4=(36);
   double diStochastic5=iStochastic(NULL,15,2,3,3,MODE_EMA,PRICE_CLOSE,MODE_MAIN,0);
   double diStochastic6=iStochastic(NULL,15,2,3,3,MODE_EMA,PRICE_CLOSE,MODE_MAIN,1);
   double diClose7=iClose(NULL,1,0);
   double diHigh8=iHigh(NULL,1,1);
   double diMACD9=iMACD(NULL,60,14,56,9,PRICE_CLOSE,MODE_MAIN,0);
   double diMACD10=iMACD(NULL,60,14,56,9,PRICE_CLOSE,MODE_MAIN,1);
   double diMACD11=iMACD(NULL,60,14,56,9,PRICE_CLOSE,MODE_MAIN,1);
   double diStochastic12=iStochastic(NULL,15,1,3,3,MODE_EMA,PRICE_CLOSE,MODE_MAIN,0);
   double d13=(66);
   double diStochastic14=iStochastic(NULL,15,1,3,3,MODE_EMA,PRICE_CLOSE,MODE_MAIN,0);
   double diStochastic15=iStochastic(NULL,15,1,3,3,MODE_EMA,PRICE_CLOSE,MODE_MAIN,1);
   double diClose16=iClose(NULL,1,0);
   double diLow17=iLow(NULL,1,1);
//----
   int total=OrdersTotal();
     if(total<1)
     {
        if(AccountFreeMargin()<(1000*Lots)){
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);
        }
        if ((diMACD0>diMACD1 && diMACD2<0 && diStochastic3<d4 && diStochastic5>diStochastic6 && diClose7>diHigh8))
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-lStopLoss*Point,0, "gordago simple",16384,0,Green);
           if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
           }
         else Print("Error opening BUY order : ",GetLastError());
         //-----
         return(0);
        }
        if ((diMACD9<diMACD10 && diMACD11>0 && diStochastic12>d13 && diStochastic14<diStochastic15 && diClose16<diLow17))
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+sStopLoss*Point,0,"gordago sample",16384,0,Red);
           if(ticket>0) 
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
           }
         else Print("Error opening SELL order : ",GetLastError());
         //----
         return(0);
        }
     }
     for(cnt=0;cnt<total;cnt++) 
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
        if(OrderType()<=OP_SELL && OrderSymbol()==Symbol()) 
        {
           if(OrderType()==OP_BUY)
           {
              if(lTrailingStop>0) 
              {
                 if(Bid-OrderOpenPrice()>Point*lTrailingStop) 
                 {
                    if(OrderStopLoss()<Bid-Point*lTrailingStop) 
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*lTrailingStop,OrderTakeProfit(),0,Green);
                     return(0);
                    }
                 }
              }
            }
            else
            {
              if(sTrailingStop>0) 
              {
                 if((OrderOpenPrice()-Ask)>(Point*sTrailingStop)) 
                 {
                    if((OrderStopLoss()>(Ask+Point*sTrailingStop)) || (OrderStopLoss()==0)) 
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*sTrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+