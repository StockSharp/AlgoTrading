//+------------------------------------------------------------------+
//|                                                sar-automated.mq4 |
//|                                  Copyright 2016, Mohammad Soubra |
//|                         https://www.mql5.com/en/users/soubra2003 |
//+------------------------------------------------------------------+

#property copyright     "Copyright 2016, Mohammad Soubra"
#property link          "https://www.mql5.com/en/users/soubra2003"
#property version       "1.00"
#property description   "This is not a profitable EA."
#property description   "This is just a basic/simple example for coders."
#property description   "This example has built in using SAR (Stop And Reverse) indicator."
#property description   "With Advanced Money Management function."
#property strict
//---
#include <stdlib.mqh>
//---
#define TradeComment "Placed by SAR example"



input int         MagicNumber    =101000;
input double      Lots           =0.1;
input double      StopLoss       =350;
input double      TakeProfit     =650;
input int         TrailingStop   =80;
input int         Slippage       =3;



//+------------------------------------------------------------------+
//    expert start function
//+------------------------------------------------------------------+
int start()
  {
   double MyPoint=Point;
   if(Digits==3 || Digits==5) MyPoint=Point*10;

   double TheStopLoss=0;
   double TheTakeProfit=0;
   
   if(TotalOrdersCount()==0)
     {
      int result=0;
      if((iSAR(NULL,0,0.02,0.2,0)<Close[0])) // Here is the open buy rule
        {
         result=OrderSend(Symbol(),OP_BUY,AdvancedMM(),Ask,Slippage,0,0,TradeComment,MagicNumber,0,Blue);
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Ask+TakeProfit*MyPoint;
            if(StopLoss>0) TheStopLoss=Ask-StopLoss*MyPoint;
            bool ordsel = OrderSelect(result,SELECT_BY_TICKET);
            bool ordmodify = OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,Green);
               if(!ordmodify) Print("Buy order has not modified with error: ", ErrorDescription(GetLastError()));
           }
           else Print("Buy order has not opened with error: ", ErrorDescription(GetLastError()));
         return(0);
        }
      if((iSAR(NULL,0,0.02,0.2,0)>Close[0])) // Here is the open Sell rule
        {
         result=OrderSend(Symbol(),OP_SELL,AdvancedMM(),Bid,Slippage,0,0,TradeComment,MagicNumber,0,Red);
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Bid-TakeProfit*MyPoint;
            if(StopLoss>0) TheStopLoss=Bid+StopLoss*MyPoint;
            bool ordsel = OrderSelect(result,SELECT_BY_TICKET);
            bool ordmodify = OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,Green);
               if(!ordmodify) Print("Sell order has not modified with error: ", ErrorDescription(GetLastError()));
           }
           else Print("Sell order has not opened with error: ", ErrorDescription(GetLastError()));
         return(0);
        }
     }

   for(int cnt=0;cnt<OrdersTotal();cnt++)
     {
      bool ordsel = OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()<=OP_SELL && 
         OrderSymbol()==Symbol() && 
         OrderMagicNumber()==MagicNumber
         )
        {
         if(OrderType()==OP_BUY)
           {
            if((iSAR(NULL,0,0.02,0.2,0)>Close[0])) //here is the close buy rule
              {
               bool ordclose = OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,Red);
              }
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>MyPoint*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-MyPoint*TrailingStop)
                    {
                     bool ordmodify = OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TrailingStop*MyPoint,OrderTakeProfit(),0,Green);
                        if(!ordmodify) Print("Buy order has not modified with error: ", ErrorDescription(GetLastError()));
                     return(0);
                    }
                 }
              }
           }
         else
           {
            if((iSAR(NULL,0,0.02,0.2,0)<Close[0])) // here is the close sell rule
              {
               bool ordclose = OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,Red);
              }
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(MyPoint*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+MyPoint*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     bool ordmodify = OrderModify(OrderTicket(),OrderOpenPrice(),Ask+MyPoint*TrailingStop,OrderTakeProfit(),0,Red);
                        if(!ordmodify) Print("Sell order has not modified with error: ", ErrorDescription(GetLastError()));
                     return(0);
                    }
                 }
              }
           }
        }
     }
   return(0);
  }
  
  
  
//+------------------------------------------------------------------+
//    expert TotalOrdersCount function
//+------------------------------------------------------------------+
int TotalOrdersCount()
  {
   int result=0;
   
   for(int i=0;i<OrdersTotal();i++)
     {
      bool ordsel = OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber()==MagicNumber) result++;

     }
   return (result);
  }
  
  
  
//+------------------------------------------------------------------+
//    expert AdvancedMM function
//+------------------------------------------------------------------+
double AdvancedMM()
  {
   int i;
   double AdvancedMMLots=0;
   bool profit1=false;
   int SystemHistoryOrders=0;
   
   for(i=0;i<OrdersHistoryTotal();i++)
     {
      bool ordsel = OrderSelect(i,SELECT_BY_POS,MODE_HISTORY);
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
//Bye.