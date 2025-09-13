//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2017, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+


extern int MagicNumber=3333;
extern double Lots=0.01;
extern double StopLoss=50;
extern double TakeProfit=30;
extern int Slippage=3;


//+------------------------------------------------------------------+
// start function                                                    |
//+------------------------------------------------------------------+
int start()
  {
   double MyPoint=Point;
   if(Digits==3 || Digits==5) MyPoint=Point*10;

   double TheStopLoss=0;
   double TheTakeProfit=0;
   if(TotalOrdersCount()==0)
     {
      int result=0;   /*bool modify;   bool select;*/
      if((iMACD(NULL,PERIOD_D1,12,26,9,PRICE_CLOSE,MODE_MAIN,0)>0) && (iMACD(NULL,PERIOD_M15,12,26,9,PRICE_CLOSE,MODE_MAIN,0)>0)) // Here is your open buy rule
        {
         TheStopLoss=0;
         TheTakeProfit=0;
         if(TakeProfit>0) TheTakeProfit=Ask+TakeProfit*MyPoint;
         if(StopLoss>0) TheStopLoss=Ask-StopLoss*MyPoint;
         result=OrderSend(Symbol(),OP_BUY,AdvancedMM(),Ask,Slippage,NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),"Example of MACD Automated",MagicNumber,0,Blue);
         /*
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Ask+TakeProfit*MyPoint;
            if(StopLoss>0) TheStopLoss=Ask-StopLoss*MyPoint;
            select=OrderSelect(result,SELECT_BY_TICKET);
            modify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,Green);
           }
         */
         return(0);
        }
      if((iMACD(NULL,PERIOD_D1,12,26,9,PRICE_CLOSE,MODE_MAIN,0)<0) && (iMACD(NULL,PERIOD_M15,12,26,9,PRICE_CLOSE,MODE_MAIN,0)<0)) // Here is your open Sell rule
        {
         TheStopLoss=0;
         TheTakeProfit=0;
         if(TakeProfit>0) TheTakeProfit=Bid-TakeProfit*MyPoint;
         if(StopLoss>0) TheStopLoss=Bid+StopLoss*MyPoint;
         result=OrderSend(Symbol(),OP_SELL,AdvancedMM(),Bid,Slippage,NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),"Example of MACD Automated",MagicNumber,0,Red);
         /*
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Bid-TakeProfit*MyPoint;
            if(StopLoss>0) TheStopLoss=Bid+StopLoss*MyPoint;
            select=OrderSelect(result,SELECT_BY_TICKET);
            modify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,Green);
           }
         */
         return(0);
        }
     }

/*
   for(int cnt=0;cnt<OrdersTotal();cnt++)
     {
      select=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()<=OP_SELL && 
         OrderSymbol()==Symbol() && 
         OrderMagicNumber()==MagicNumber
         )
        {
         if(OrderType()==OP_BUY)
           {
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>MyPoint*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-MyPoint*TrailingStop)
                    {
                     modify=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TrailingStop*MyPoint,OrderTakeProfit(),0,Green);
                     return(0);
                    }
                 }
              }
           }
         else if(OrderType()==OP_SELL)
           {
            if(TrailingStop>0)
              {
               if(OrderOpenPrice()-Ask>MyPoint*TrailingStop)
                 {
                  if(OrderStopLoss()>Ask+MyPoint*TrailingStop)
                    {
                     modify=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+MyPoint*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }
           }
        }
     }
*/
   return(0);
  }
  
  
//+------------------------------------------------------------------+
//| Function TotalOrdersCount()                                      |
//+------------------------------------------------------------------+
int TotalOrdersCount()
  {
   int result=0;
   for(int i=0;i<OrdersTotal();i++)
     {
      bool select=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber()==MagicNumber) result++;

     }
   return (result);
  }
  
  
//+------------------------------------------------------------------+
//| Function AdvancedMM()                                            |
//+------------------------------------------------------------------+
double AdvancedMM()
  {
   int i;
   double AdvancedMMLots=0;
   bool profit1=false;
   int SystemHistoryOrders=0;
   for(i=0;i<OrdersHistoryTotal();i++)
     {
      bool select=OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);
      if(OrderMagicNumber()==MagicNumber) SystemHistoryOrders++;
     }
   bool profit2=false;
   int LO=0;
   if(SystemHistoryOrders<2) return(Lots);
   for(i=OrdersHistoryTotal()-1;i>=0;i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
         if(OrderMagicNumber()==MagicNumber)
           {
            if(OrderProfit()>=0 && profit1) return(Lots);
            if(LO==0)
              {
               if(OrderProfit()>=0) profit1=true;
               if(OrderProfit()<0)  return(OrderLots());
               LO=1;
              }
            if(OrderProfit()>=0 && profit2) return(AdvancedMMLots);
            if(OrderProfit()>=0) profit2=true;
            if(OrderProfit()<0)
              {
               profit1=false;
               profit2=false;
               AdvancedMMLots+=OrderLots();
              }
           }
     }
   return(AdvancedMMLots);
  }
  
  
//+------------------------------------------------------------------+
