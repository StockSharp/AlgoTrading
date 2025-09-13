//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2012, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""
#include <stdlib.mqh>
#include <WinUser32.mqh>
extern double stoploss=40;
extern double takeprofit=200;
extern string distance="trailing starts after reaching x pips";
extern double trailingstart=30;
extern string trailstop="Trailing stop pips - distance from current price";
extern double trailingstop=20;
extern string toBreakEven=" breakeven after reaching x pips";
extern double breakevengain=25;
extern string breakevenpips="x pips locked in profit";
extern double breakeven=10;
double PipValue;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   return(INIT_SUCCEEDED);
  }
//--- Expert start
int start()
  {
   OnTick();
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   double NDigits=Digits;
   if(NDigits==3 || NDigits==5)
     {PipValue=10;}
   else {PipValue=1;}
//---
   SetSLTP();
   Movebreakeven();
   TrailingStop();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetSLTP()
  {
//---
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result1=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stoploss*PipValue*Point,OrderOpenPrice()+takeprofit*PipValue*Point,0,Blue);
            if(!result1 && (GetLastError()>0))Print("Error in OrderModify res1. Error code=",GetLastError());
           }
         //---
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result2=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stoploss*PipValue*Point,OrderOpenPrice()-takeprofit*PipValue*Point,0,Red);
            if(!result2 && (GetLastError()>0))Print("Error in OrderModify res2. Error code=",GetLastError());
           }
         //---
         if((OrderType()==OP_BUYLIMIT || OrderType()==OP_BUYSTOP) && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result3=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stoploss*PipValue*Point,OrderOpenPrice()+takeprofit*PipValue*Point,0,Blue);
            if(!result3 && (GetLastError()>0))Print("Error in OrderModify res3. Error code=",GetLastError());
           }
         //---
         if((OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP) && OrderSymbol()==Symbol() && (OrderStopLoss()==0 || OrderTakeProfit()==0))
           {
            bool result4=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stoploss*PipValue*Point,OrderOpenPrice()-takeprofit*PipValue*Point,0,Red);
            if(!result4 && (GetLastError()>0))Print("Error in OrderModify res4. Error code=",GetLastError());
           }
        }
   else
     {
      Print("OrderSelect() error - ",ErrorDescription(GetLastError()));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Movebreakeven()
  {
   RefreshRates();
//---
   if(OrdersTotal()>0)
     {
      for(int i=OrdersTotal();i>=0;i--)
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)
                 {
                  if(Bid-OrderOpenPrice()>=breakevengain*PipValue*Point)
                    {
                     if((OrderStopLoss()-OrderOpenPrice()<breakeven*PipValue*Point) || OrderStopLoss()==0)
                       {
                        bool result6=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+breakeven*PipValue*Point,OrderTakeProfit(),0,Blue);
                        if(!result6 && (GetLastError()>0))Print("Error in OrderModify res6. Error code=",GetLastError());
                       }
                    }
                 }
               else
                 {
                  if(OrderOpenPrice()-Ask>=breakevengain*PipValue*Point)
                    {
                     if((OrderOpenPrice()-OrderStopLoss()<breakeven*PipValue*Point) || OrderStopLoss()==0)
                       {
                        bool result7=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-breakeven*PipValue*Point,OrderTakeProfit(),0,Red);
                        if(!result7 && (GetLastError()>0))Print("Error in OrderModify res7. Error code=",GetLastError());
                       }

                    }
                 }
              }
           }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingStop()
  {
   RefreshRates();
//---
   if(OrdersTotal()>0)
     {
      for(int i=OrdersTotal();i>=0;i--)
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)
                 {
                  if(Ask>(OrderOpenPrice()+trailingstart*PipValue*Point) && OrderStopLoss()<Bid-(trailingstop+0.001)*PipValue*Point)
                    {
                     bool result8=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-trailingstop*PipValue*Point,OrderTakeProfit(),0,Blue);
                     if(!result8 && (GetLastError()>0))Print("Error in OrderModify res8. Error code=",GetLastError());
                    }
                 }
               else
                 {
                  if((Bid<OrderOpenPrice()-trailingstart*PipValue*Point && OrderStopLoss()>Ask+(trailingstop+0.001)*PipValue*Point) || (OrderStopLoss()==0))
                    {
                     bool result9=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+trailingstop*PipValue*Point,OrderTakeProfit(),0,Red);
                     if(!result9 && (GetLastError()>0))Print("Error in OrderModify res9. Error code=",GetLastError());
                    }

                 }
              }
           }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
