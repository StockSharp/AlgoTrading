//+------------------------------------------------------------------+
//|                                                cci-automated.mq4 |
//|                                  Copyright 2016, Mohammad Soubra |
//|                         https://www.mql5.com/en/users/soubra2003 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Mohammad Soubra"
#property link      "https://www.mql5.com/en/users/soubra2003"
#property version   "1.00"
#property strict
input string separator1="--------------- TRADES OPTIONS ---------------";//TRADING INPUTS >> >> >> >>
input int    TradesDuplicator=3;//Trades Duplicator
input double Lots=0.03;//Fixed Lot Size
input int    MagicNumber=1982;//Trades ID
input double StopLoss=50;//Stop Loss
input double TakeProfit=200;//Take Profit
input int    TrailingStop=50;//Trailing Stop
input int    Slippage=3;
input string separator2="--------------- CCI OPTIONS ---------------";//CCI INPUTS >> >> >> >>
input int    CCIPeriod=9;//CCI Period
input string separator3="--------------- ON CHART COLORS ---------------";//TRADES ARROW COLOR >> >> >> >>
input color  BuyArrowOpen=clrBlue;//Buy Arrow Color
input color  SellArrowOpen=clrRed;//Sell Arrow Color
input color  ModificationArrow=clrWhite;//Modified Trades Arrow Color
//+------------------------------------------------------------------+
//|  expert OnTick: main function                                    |
//+------------------------------------------------------------------+
void OnTick()
  {
   double CoderPoint=Point;
   if(Digits==3 || Digits==5) CoderPoint=Point*10;
//---
   double CCI_Typical_Curr=iCCI(NULL,0,CCIPeriod,PRICE_TYPICAL,0);
   double CCI_Typical_Prev=iCCI(NULL,0,CCIPeriod,PRICE_TYPICAL,1);
//---
   double TheStopLoss=0;
   double TheTakeProfit=0;
   if(TotalOrdersCount()<=TradesDuplicator)
     {
      int result=0;
      if((CCI_Typical_Prev<-90) && CCI_Typical_Curr>-80) // Here is the open buy rule
        {
         result=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,NULL,NULL,"coding only, not to trade",MagicNumber,0,BuyArrowOpen);
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Ask+TakeProfit*CoderPoint;
            if(StopLoss>0) TheStopLoss=Ask-StopLoss*CoderPoint;
            bool OrdSel=OrderSelect(result,SELECT_BY_TICKET);
            bool OrdMod=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,ModificationArrow);
           }
        }
      if((CCI_Typical_Prev>90) && (CCI_Typical_Curr<80)) // Here is the open Sell rule
        {
         result=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,NULL,NULL,"coding only, not to trade",MagicNumber,0,SellArrowOpen);
         if(result>0)
           {
            TheStopLoss=0;
            TheTakeProfit=0;
            if(TakeProfit>0) TheTakeProfit=Bid-TakeProfit*CoderPoint;
            if(StopLoss>0) TheStopLoss=Bid+StopLoss*CoderPoint;
            bool OrdSel1=OrderSelect(result,SELECT_BY_TICKET);
            bool OrdMod1=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TheStopLoss,Digits),NormalizeDouble(TheTakeProfit,Digits),0,ModificationArrow);
           }
        }
     }
   for(int cnt=0;cnt<OrdersTotal();cnt++)
     {
      bool OrdSel2=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()<=OP_SELL && 
         OrderSymbol()==Symbol() && 
         OrderMagicNumber()==MagicNumber)
        {
         if(OrderType()==OP_BUY)
           {
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>CoderPoint*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-CoderPoint*TrailingStop)
                    {
                     bool OrdMod2=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TrailingStop*CoderPoint,OrderTakeProfit(),0,ModificationArrow);
                    }
                 }
              }
           }
         else
           {
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(CoderPoint*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+CoderPoint*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     bool OrdMod3=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+CoderPoint*TrailingStop,OrderTakeProfit(),0,ModificationArrow);
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|  expert function: TotalOrdersCount                               |
//+------------------------------------------------------------------+
int TotalOrdersCount()
  {
   int result=1;
   for(int i=0;i<OrdersTotal();i++)
     {
      bool OrdSel=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber()==MagicNumber)
         {
            result++;
         }
     }
   return (result);
  }
//+------------------------------------------------------------------+
