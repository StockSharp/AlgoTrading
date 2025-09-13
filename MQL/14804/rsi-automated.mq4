//+------------------------------------------------------------------+
//|                                                rsi-automated.mq4 |
//|                                  Copyright 2016, Mohammad Soubra |
//|                                        https://www.onesoubra.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Mohammad Soubra"
#property link      "https://www.mql5.com/en/users/soubra2003"
#property version   "1.00"
#property strict

input int MagicNumber=1982;  //Magic Number
input double Lots=0.01;      //Fixed Lots
input double StopLoss=50;    //Fixed Stop Loss (in Points)
input double TakeProfit=150; //Fixed Take Profit (in Points)
input int TrailingStop=25;   //Trailing Stop (in Points)
input int Slippage=3;
//+------------------------------------------------------------------+
//|   expert OnTick function                                         |
//+------------------------------------------------------------------+
void OnTick()
  {
   double MyPoint=Point;
   if(Digits==3 || Digits==5) MyPoint=Point*10;

   double TheStopLoss=0;
   double TheTakeProfit=0;
   if(TotalOrdersCount()==0)
     {
      int result=0;
      if((iRSI(NULL,0,14,PRICE_CLOSE,0)<25)) // Here is the open buy condition
        {
         result=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,0,0,"EXAMPLE OF RSI AUTOMATED",MagicNumber,0,Blue);
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Ask+TakeProfit*MyPoint;
            if(StopLoss>0) TheStopLoss=Ask-StopLoss*MyPoint;
            int MyOrderSelect=OrderSelect(result,SELECT_BY_TICKET);
            int MyOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,Green);
           }
        }
      if((iRSI(NULL,0,14,PRICE_CLOSE,0)>75)) // Here is the open Sell condition
        {
         result=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,0,0,"EXAMPLE OF RSI AUTOMATED",MagicNumber,0,Red);
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Bid-TakeProfit*MyPoint;
            if(StopLoss>0) TheStopLoss=Bid+StopLoss*MyPoint;
            int MyOrderSelect=OrderSelect(result,SELECT_BY_TICKET);
            int MyOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,Green);
           }
        }
     }

   for(int cnt=0;cnt<OrdersTotal();cnt++)
     {
      int MyOrderSelect=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()<=OP_SELL && 
         OrderSymbol()==Symbol() && 
         OrderMagicNumber()==MagicNumber
         )
        {
         if(OrderType()==OP_BUY)
           {
            if((iRSI(NULL,0,14,PRICE_CLOSE,0)>50)) //here is the close buy condition
              {
               int MyOrderClose=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,Red);
              }
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>MyPoint*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-MyPoint*TrailingStop)
                    {
                     int MyOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TrailingStop*MyPoint,OrderTakeProfit(),0,Green);
                    }
                 }
              }
           }
         else
           {
            if((iRSI(NULL,0,14,PRICE_CLOSE,0)<50)) // here is the close sell condition
              {
               int MyOrderClose=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),Slippage,Red);
              }
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(MyPoint*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+MyPoint*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     int MyOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+MyPoint*TrailingStop,OrderTakeProfit(),0,Red);
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|   expert TotalOrdersCount function                               |
//+------------------------------------------------------------------+
int TotalOrdersCount()
  {
   int result=0;
   for(int i=0;i<OrdersTotal();i++)
     {
      int MyOrderSelect=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber()==MagicNumber) result++;

     }
   return (result);
  }
//-------------------------------------------------------------------+
//Bye
